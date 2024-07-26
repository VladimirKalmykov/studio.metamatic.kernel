using NUnit.Framework;

namespace Metamatic.EditMode.Validator
{
    [TestFixture]
    public class ValidatorTests
    {
        [Test]
        public void Validator_AddValidator_ReturnsTrue()
        {
            // Arrange
            var validator = new Validator<int>();
            var result = false;
            
            // Act
            using (validator.Add(i => i > 0))
            {
                result = validator.Validate(1);
            }

            // Assert
            Assert.IsTrue(result);
        }
        
        
        [Test]
        public void Validator_AddValidator_ReturnsFalse()
        {
            // Arrange
            var validator = new Validator<int>();
            var result = false;
            
            // Act
            using (validator.Add(i => i > 0))
            {
                result = validator.Validate(-1);
            }

            // Assert
            Assert.IsFalse(result);
        }
        
        [Test]
        public void Validator_AddMultipleValidators_ReturnsTrue()
        {
            // Arrange
            var validator = new Validator<int>();
            var result = false;
            
            // Act
            using (validator.Add(i => i > 0))
            using (validator.Add(i => i < 10))
            {
                result = validator.Validate(5);
            }

            // Assert
            Assert.IsTrue(result);
        }
        
        [Test]
        public void Validator_AddMultipleValidators_ReturnsFalse()
        {
            // Arrange
            var validator = new Validator<int>();
            var result1 = false;
            var result2 = false;
            
            // Act
            using (validator.Add(i => i > 0))
            using (validator.Add(i => i < 10))
            {
                result1 = validator.Validate(15);
                result2 = validator.Validate(-5);
            }

            // Assert
            Assert.IsFalse(result1);
            Assert.IsFalse(result2);
        }
        
        [Test]
        public void Validator_DisposeValidator_ReturnsTrue()
        {
            // Arrange
            var validator = new Validator<int>();
            var result = false;
            
            // Act
            using (validator.Add(i => i < 0))
            {
                result = validator.Validate(1);
            }

            // Assert
            Assert.IsFalse(result);
            
            // Act
            result = validator.Validate(1);
            
            // Assert
            Assert.IsTrue(result);
        }
        
        [Test]
        public void Validator_DisposeOneOfValidators_ReturnsTrue()
        {
            // Arrange
            var validator = new Validator<int>();
            var result = false;
            
            // Act
            var dispose1 = validator.Add(i => i > 0);
            var dispose2 = validator.Add(i => (i % 2) == 0);
            var dispose3 = validator.Add(i => i < 5);
            
            result = validator.Validate(1);
            Assert.IsFalse(result);
            
            // Act
            dispose2.Dispose();
            
            // Assert
            result = validator.Validate(1);
            Assert.IsTrue(result);
            
            dispose1.Dispose();
            dispose3.Dispose();
        }
        
        [Test]
        public void Validator_DisposeAllValidators_ReturnsTrue()
        {
            // Arrange
            var validator = new Validator<int>();
            var result = false;
            
            // Act
            var dispose1 = validator.Add(i => i > 0);
            var dispose2 = validator.Add(i => (i % 2) == 0);
            var dispose3 = validator.Add(i => i < 5);
            
            result = validator.Validate(1);
            Assert.IsFalse(result);
            
            // Act
            dispose1.Dispose();
            dispose2.Dispose();
            dispose3.Dispose();
            
            // Assert
            result = validator.Validate(1);
            Assert.IsTrue(result);
        }

        [Test]
        public void Validator_ValidatePerformanceTest()
        {
            // Arrange
            var validator = new Validator<int>();
            var result = false;
            var elapsed = System.TimeSpan.Zero;
            // Act
            using (validator.Add(i => i > 0))
            {
                var startTime = System.DateTime.Now;
                for (var i = 0; i < 1000000; i++)
                {
                    result = validator.Validate(5);
                }
                var endTime = System.DateTime.Now;
                elapsed = endTime - startTime;
                UnityEngine.Debug.Log($"Elapsed: {elapsed.Milliseconds} ms");
            }

            Assert.IsTrue(elapsed < System.TimeSpan.FromMilliseconds(25));
        }
        
        [Test]
        public void Validator_ValidatesPerformanceTest()
        {
            // Arrange
            var validator = new Validator<int>();
            var resultValidator = false;
            var elapsedValidator = System.TimeSpan.Zero;
            var elapsedInline = System.TimeSpan.Zero;
            // Act
            using (validator.Add(i => i > 0))
            using (validator.Add(i => i > 1))
            using (validator.Add(i => i > 2))
            using (validator.Add(i => i > 3))
            using (validator.Add(i => i > 4))
            using (validator.Add(i => i > 5))
            using (validator.Add(i => i > 6))
            using (validator.Add(i => i > 7))
            using (validator.Add(i => i > 8))
            using (validator.Add(i => i >= 9))
            using (validator.Add(i => i < 10))
            {
                var startTime = System.DateTime.Now;
                for (var i = 0; i < 1000000; i++)
                {
                    validator.Validate(9);
                }
                var endTime = System.DateTime.Now;
                elapsedValidator = endTime - startTime;
                UnityEngine.Debug.Log($"Validator elapsed: {elapsedValidator.Milliseconds} ms");
            }
            

            Assert.IsTrue(elapsedValidator < System.TimeSpan.FromMilliseconds(140));
        }

        [Test]
        public void Validator_AddRemoveValidators_PerformanceTest()
        {
            // Arrange
            var validator = new Validator<int>();
            var resultValidator = false;
            var elapsedValidator = System.TimeSpan.Zero;
            var elapsedInline = System.TimeSpan.Zero;
            // Act
            var startTime = System.DateTime.Now;
            for (var i = 0; i < 1000000; i++)
            {
                using (validator.Add(i => i > 0))
                {
                    resultValidator = validator.Validate(5);
                }
            }
            var endTime = System.DateTime.Now;
            elapsedValidator = endTime - startTime;
            UnityEngine.Debug.Log($"Validator elapsed: {elapsedValidator.Milliseconds} ms");
            
            Assert.IsTrue(elapsedValidator < System.TimeSpan.FromMilliseconds(25));
        }
    }
}