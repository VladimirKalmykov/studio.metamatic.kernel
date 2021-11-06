using UnityEngine;
using UnityEngine.Serialization;

namespace Metamatic.UI
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    [AddComponentMenu("Metamatic/UI/Particle System")]
    [RequireComponent(typeof(CanvasRenderer), typeof(ParticleSystem))]
    public class CanvasParticleSystem : MaskableGraphic
    {
        public Texture particleImage;
        public UIParticlesSortMode sortMode;
        public CanvasParticleSystemRenderMode renderMode;

        public float speedScale = 0f;
        public float lengthScale = 1f;

        public Vector3 pivot = Vector3.zero;
        public bool scaleShapeByRectTransform;
        public bool updateMaterialDirty;

        private bool m_Initialized;
        private RectTransform m_RectTransform;

        private ParticleSystem m_ParticleSystem;
        private ParticleSystemRenderer m_ParticleSystemRenderer;

        // отдельные модули системы частиц, для удобства
        private ParticleSystem.MainModule m_Main;
        private ParticleSystem.TextureSheetAnimationModule m_TextureSheetAnimation;
        private ParticleSystem.ShapeModule m_Shape;

        // частицы
        private ParticleSystem.Particle[] m_Particles;
        private float[] m_ParticlesLifeProgress;
        private float[] m_ParticleElapsedLifetime;

        private Vector3[] m_QuadCorners = new Vector3[]
        {
            new Vector3(-.5f, -.5f, 0),
            new Vector3(-.5f, .5f, 0),
            new Vector3(.5f, .5f, 0),
            new Vector3(.5f, -.5f, 0)
        };

        private Vector2[] m_SimpleUV = new Vector2[]
        {
            new Vector2(0,0),
            new Vector2(0,1),
            new Vector2(1,1),
            new Vector2(1,0),
        };


        public override Texture mainTexture
        {
            get { return particleImage; }
        }


        protected void Initialize()
        {
            if (m_Initialized)
                return;
            m_Initialized = true;

            m_RectTransform = GetComponent<RectTransform>();
            m_ParticleSystem = GetComponent<ParticleSystem>();

            m_Main = m_ParticleSystem.main;
            m_TextureSheetAnimation = m_ParticleSystem.textureSheetAnimation;
            m_Shape = m_ParticleSystem.shape;

            m_ParticleSystemRenderer = GetComponent<ParticleSystemRenderer>();
            m_ParticleSystemRenderer.enabled = false;
            m_ParticleSystemRenderer.material = null;

            var maxCount = m_Main.maxParticles;
            m_Particles = new ParticleSystem.Particle[maxCount];

            m_ParticlesLifeProgress = new float[maxCount];
            m_ParticleElapsedLifetime = new float[maxCount];
        }

        protected override void Awake()
        {
            base.Awake();
            Initialize();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ScaleShape();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
    #if UNITY_EDITOR
            Initialize();
    #endif

            vh.Clear();
            if (m_Particles == null) {
                return;
            }
            var particlesCount = m_ParticleSystem.GetParticles(m_Particles);
            for (var i = 0; i < particlesCount; i++)
            {
                m_ParticlesLifeProgress[i] = 1f - m_Particles[i].remainingLifetime / m_Particles[i].startLifetime;
                m_ParticleElapsedLifetime[i] = m_Particles[i].startLifetime - m_Particles[i].remainingLifetime;
            }

            switch (sortMode)
            {
                case UIParticlesSortMode.NONE: break;
                case UIParticlesSortMode.OLDEST_IN_FRONT:
                    Array.Sort(m_ParticleElapsedLifetime, m_Particles, 0, particlesCount, Comparer<float>.Default);
                    Array.Reverse(m_Particles, 0, particlesCount);
                    break;
                case UIParticlesSortMode.YOUNGEST_IN_FRONT:
                    Array.Sort(m_ParticleElapsedLifetime, m_Particles, 0, particlesCount, Comparer<float>.Default);
                    break;
            }

            var animationFramesCount = 1;
            var animationFrameSize = Vector2.one;

            if (m_TextureSheetAnimation.enabled)
            {
                animationFramesCount = m_TextureSheetAnimation.numTilesX * m_TextureSheetAnimation.numTilesY;
                animationFrameSize = new Vector2(1f / m_TextureSheetAnimation.numTilesX, 1f / m_TextureSheetAnimation.numTilesY);
            }

            for (var i = 0; i < particlesCount; i++)
            {
                var particle = m_Particles[i];

                // Position
                Vector3 particlePosition;
                switch (m_Main.simulationSpace)
                {
                    case ParticleSystemSimulationSpace.World:
                        particlePosition = m_RectTransform.InverseTransformPoint(particle.position);
                        break;
                    case ParticleSystemSimulationSpace.Local:
                        particlePosition = particle.position;
                        break;
                    case ParticleSystemSimulationSpace.Custom:
                        if (m_Main.customSimulationSpace != null)
                            particlePosition = m_RectTransform.InverseTransformPoint(m_Main.customSimulationSpace.TransformPoint(particle.position));
                        else
                            particlePosition = particle.position;
                        break;
                    default:
                        particlePosition = particle.position;
                        break;
                }

                // Color
                var vertexColor = particle.GetCurrentColor(m_ParticleSystem) * color;

                // Size
                var particleSize = particle.GetCurrentSize3D(m_ParticleSystem);


                // UV & Animation
                Vector2[] vertexUV = null;

                if (m_TextureSheetAnimation.enabled)
                {
                    var frameProgress = m_TextureSheetAnimation.frameOverTime.Evaluate(m_ParticlesLifeProgress[i]);

                    frameProgress = Mathf.Repeat(frameProgress * m_TextureSheetAnimation.cycleCount, 1);
                    var frame = 0;

                    switch (m_TextureSheetAnimation.animation)
                    {

                        case ParticleSystemAnimationType.WholeSheet:
                            frame = Mathf.FloorToInt(frameProgress * animationFramesCount);
                            break;
                        case ParticleSystemAnimationType.SingleRow:
                            frame = Mathf.FloorToInt(frameProgress * m_TextureSheetAnimation.numTilesX);
                            frame += m_TextureSheetAnimation.rowIndex * m_TextureSheetAnimation.numTilesX;
                            break;
                    }

                    frame %= animationFramesCount;

                    Vector4 particleUV;


                    //particleUV.x = (frame % _textureSheetAnimation.numTilesX) * animationFrameSize.x;
                    //particleUV.y = Mathf.FloorToInt(frame / _textureSheetAnimation.numTilesX) * animationFrameSize.y;
                    //particleUV.z = particleUV.x + animationFrameSize.x;
                    //particleUV.w = particleUV.y + animationFrameSize.y;
                    //vertexUV = RectToUV(particleUV);

                    // reverse uv
                    particleUV.x = (frame % m_TextureSheetAnimation.numTilesX) * animationFrameSize.x;
                    particleUV.y = 1 - Mathf.FloorToInt(frame / m_TextureSheetAnimation.numTilesX) * animationFrameSize.y;
                    particleUV.z = particleUV.x + animationFrameSize.x;
                    particleUV.w = particleUV.y - animationFrameSize.y;
                    vertexUV = RectToUVFlipV(particleUV);
                }
                else
                    vertexUV = m_SimpleUV;


                Quaternion rotation;
                switch (renderMode)
                {
                    case CanvasParticleSystemRenderMode.BILLBOARD:
                        rotation = Quaternion.AngleAxis(particle.rotation, Vector3.forward);
                        break;
                    case CanvasParticleSystemRenderMode.STRETCHED_BILLBOARD:
                        rotation = Quaternion.LookRotation(Vector3.forward, particle.totalVelocity);
                        var speed = particle.totalVelocity.magnitude;                    
                        particleSize = Vector3.Scale(particleSize, new Vector3(lengthScale + speed * speedScale, 1f, 1f));
                        rotation *= Quaternion.AngleAxis(90, Vector3.forward);
                        break;
                    default:
                        rotation = Quaternion.AngleAxis(particle.rotation, Vector3.forward);
                        break;
                }
                var quadVerts = new UIVertex[4];

                for (var j = 0; j < 4; j++)
                {
                    var cornerPosition = Vector3.Scale(particleSize, m_QuadCorners[j] + pivot);
                    var vertexPosition = rotation * cornerPosition + particlePosition;
                    vertexPosition.z = 0;

                    quadVerts[j] = new UIVertex();
                    quadVerts[j].color = vertexColor;
                    quadVerts[j].uv0 = vertexUV[j];
                    quadVerts[j].position = vertexPosition;
                }

                vh.AddUIVertexQuad(quadVerts);
            }
        }

        protected void Update()
        {
            SetVerticesDirty();
            if (updateMaterialDirty)
                SetMaterialDirty();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            Initialize();
            base.OnRectTransformDimensionsChange();
            ScaleShape();
        }

        protected void ScaleShape()
        {
            if (!scaleShapeByRectTransform)
                return;

            var rect = m_RectTransform.rect;
            var scale = Quaternion.Euler(m_Shape.rotation) * new Vector3(rect.width, rect.height, 0);
            scale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            m_Shape.scale = scale;
        }


        private Vector2[] RectToUV(Vector4 uvRect)
        {
            return new Vector2[]
                {
                    new Vector2(uvRect.x,uvRect.y),
                    new Vector2(uvRect.x,uvRect.w),
                    new Vector2(uvRect.z,uvRect.w),
                    new Vector2(uvRect.z,uvRect.y),
                };
        }

        // if uvRect.w < uvRect.y
        private Vector2[] RectToUVFlipV(Vector4 uvRect)
        {
            return new Vector2[]
                {
                    new Vector2(uvRect.x,uvRect.w),
                    new Vector2(uvRect.x,uvRect.y),
                    new Vector2(uvRect.z,uvRect.y),
                    new Vector2(uvRect.z,uvRect.w),
                };
        }
    }

    public enum UIParticlesSortMode
    {
        NONE = 0,
        OLDEST_IN_FRONT = 1,
        YOUNGEST_IN_FRONT = 2
    }

    public enum CanvasParticleSystemRenderMode
    {
        BILLBOARD = 0,
        STRETCHED_BILLBOARD = 1
    }

}