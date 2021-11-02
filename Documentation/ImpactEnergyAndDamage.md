# Impact energy and damage

# Theory

Energy (**j/cc**) = joules per cubic centimeter

Interesting articles: [https://cablescalculations.wordpress.com/2018/03/23/energy-required-to-destroy-rock/](https://cablescalculations.wordpress.com/2018/03/23/energy-required-to-destroy-rock/)

**Compressive strength** - the ability of material to take pressure without fracturing

I assume that hit energy is `(Mathf.Pow(velocityMagnitude, 2) * mass) / 2f` (*Mass times the speed of light squared equals energy*)

> 

> In classical mechanics, kinetic energy (KE) is equal to half of an object's mass (1/2*m) multiplied by the velocity squared. For example, if a an object with a mass of 10 kg (m = 10 kg) is moving at a velocity of 5 meters per second (v = 5 m/s), the kinetic energy is equal to 125 Joules, or (1/2 * 10 kg) * 5 m/s2.


**We use Joules, kilograms, and meters per second as our defaults**

Well-done calculator of asteroid destruction: [http://www.stardestroyer.net/Empire/Science/Asteroids.html](http://www.stardestroyer.net/Empire/Science/Asteroids.html)

----------------------------------------------------------------

It says that to destroy asteroid of **Ice** with diameter **10 meters** (density of Ice 916 kg/m3) with mass **1220 tons** (*megagrams*) into small peaces (1 meter) needs **1 ton** of TNT equivalent.

According to the table of TNT equivalents I assume that it needs **4.184 gigajoule** or `4184000` kilojoule or `4184000000` joule

----------------------------------------------------------------

```
milligram of TNT	mg	nanoton of TNT	nt	4.184 J or 4.184 joules	1.162 mWh	46.55 fg
gram of TNT	g	microton of TNT	μt	4.184×103 J or 4.184 kilojoules	1.162 Wh	46.55 pg
kilogram of TNT	kg	milliton of TNT	mt	4.184×106 J or 4.184 megajoules	1.162 kWh	46.55 ng
megagram of TNT	Mg	ton of TNT	t	4.184×109 J or 4.184 gigajoules	1.162 MWh	46.55 μg
gigagram of TNT	Gg	kiloton of TNT	kt	4.184×1012 J or 4.184 terajoules	1.162 GWh	46.55 mg
teragram of TNT	Tg	megaton of TNT	Mt	4.184×1015 J or 4.184 petajoules	1.162 TWh	46.55 g
petagram of TNT	Pg	gigaton of TNT	Gt	4.184×1018 J or 4.184 exajoules	1.162 PWh	46.55 kg
```

**Joule conversion system**:

```
1 kilojoule = 1000 joule
1 megajoule = 1000000 joule
1 gigajoule = 1000000000 joule
```

Thus, to destroy into pieces Ice cube of mass 1kg needs energy `4184000000 / 1220000` **~3429.5 joule**

Also, the [stardestroyer](http://www.stardestroyer.net/Empire/Science/Asteroids.html) says that

> 1 kiloton buried explosive will fragment a 100m diameter asteroid

So **4000 gigajoules needs to destroy to the pieces 1 meter diameter asteroid** (1 kilotonn gives 4184 gigajoules)

> Y = (d/100)³, where d is diameter in metres and Y is yield in kilotons

So, the formula to get energy to destroy 100 meters of asteroid is `E(terajoule) = (d / 100)³ * 4.184`

The volume of spherical asteroid with diameter 100 meters (by formula *V=43πr3*) is `(4/3) * 3.14 * Math.pow(100, 3)` is `4186666` (Strange, but Unity physics engine assumes that it will be 333333, so be it)

I can now rephrase: **4000 gigajoules needs to destroy to the pieces asteroid with volume 333333 meters**

This formula is valid for Igneous Rocks. Depends on the page https://www2.nau.edu/~gaud/RiodeFlag/ignsrck.htm igneous rock contains approximately *A basalt is about 53% SiO2, whereas granite is 73%.*

```
Rock Type density range (g/cm )

Rhyolite: 2.35 - 2.70
Granite: 2.50 - 2.81
Andesite: 2.40 - 2.80
Basalt: 2.70 - 3.30
Gabbro: 2.70 - 3.50
```

I can admin convention that **igneous rock density is 3000 kg/m3**

Acording to formula `mass = volume * density` igneous rock asteroid with volume 4186666 meters is about `333333 * 3000 = 999999000 kg = 1 million ton`, so it can be sayed as:


**4000 gigajoules needs to destroy to the pieces asteroid with mass 1 million ton**

Let's find formula for igneous rock:

`4000 gigajoule = 1000000000kg / 250000`
`E (gigajoule) = mass / 250000`
`E (joule) = mass / 2500000 * 1000000000` 
`E joule = mass / 0.00025`
`E joule = mass * 4000`

---------------------------------

**E joule = mass * 4000**

---------------------------------

Let's test this formula on origin statement *"1 kiloton buried explosive will fragment a 100m diameter asteroid"*

`1 kiloton TNT = 4184 gigajoules = 4184,000,000,000 joules = 333333 m³ * 3000 * 4000`

Well done.

Now check the statement *" to destroy asteroid of Ice with diameter 10 meters (density of Ice 916 kg/m3) with mass 1220 tons (megagrams) into small peaces (1 meter) needs 1 ton of TNT equivalent."*

`1 ton TNT = 4.184 gigajoule = 4184000000 joule = 1220000 kg * 400 != 488000000 joule`

Results are not equal. Formula's result is smaller than statement's result at least 10 times less. May be it is because the the statement is valid only for igneous rock. Let's calculate use the same formula and condition but with igneous rock instead.

`1 ton TNT = 4.184 gigajoule = 4184000000 joule = ((4 / 3) * 3.14 * Math.pow(10, 3)) * 400 = 4186 * 3000 * 400 ~= 5023200000 joule`

Close enough!

Thus, I think it's normal that Ice asteroid requires less energy to be exposed than igneous rock, because Ice density is really small.

At least, the all above statements are valid only for complete destruction (into small pieces), but in gameplay, it will be enough to split asteroids into a few pieces that require even less energy than total annihilation. And some materials are more prone to fragmentation than others.

## Type of destructions

It can be assumed that there are several types of destruction of an object.

### Broking

It's when the object splits into a few parts. Each of them becomes a separated object. This kind of destruction is followed with less explosion effect. To be broken it's enough to take 1 / 2 of full-destruction energy, specifically this energy is calculated as `brokeE = health`

### Fragmentation

The object explodes to many small parts. Accompanied by a beautiful explosion. These parts are too small to be consumed as objects. Probably, I need to render these parts with particle system. The formula to get this kind of dustructions is `fragmentationE = health * 2`

### Vaporization

The object explodes without residue. Total annihilation. We can observe only a cloud of hot gas without any pieces. Explosion effect is maximum: HDR light, plasma bow, and very small particles.

*The required energy to vaporize igneous rock 10m³ is 3.8 kilotons = 4.184 terajoules = 4184000000000 joules - this is 3 thousand times more fragmentation energy*

So formula to get vaporization is `vaporizationE = health * 2 * 3000`

# Health

## Health definitions

There're two kind of health source.

### Calculated from object's mass

Depending on this Elephant has more health than mouse because it has a bigger mass. A granite cube has more health than ice cube the same size, because density of granite is higher than ice.

### Fixed health

The fixed health value is assigned manually. In this case, health is independent of mass. You are able to create a mouse with the health of an elephant, vice versa.

## Health modifiers

To reduce the complexity of an object's destruction energy calculation there's a global setting's property `PhysicalBodySettings.healthMultiplier` that equals to `2000`. Every calculated from mass health or fixed health will be multiplied by _healthMultiplier_.

For example, the ball with weight _1 kg_ has health _2000 hp_ and it's required _2000 joules_ to destroy it. The painless calculations, right? Also, an asteroid with a mass of 1 million tons will have health *2,000,000,000,000* and it requires *2,000,000,000,000* joules to be destroyed.

These numbers are very close to real physical values.

# Collision energy

`Kinetic energy (joule) = mass (kg) pow 2 multiplied to velocity (m/sec) separated by 2.`

# Taking damage

When an object's under attack with specific energy it should lose its health. For example, in a game, you can destroy an asteroid by shooting him with a machine gun. By not in the life. In life, there's no such concept as "losing health by damage". You can break the leg, you split the part of the stone, but you can't reduce the "health" of the stone. But in games health is the only way to make nice gameplay. The question is how much health is taken by kinetic energy.

The damage of kinetic energy (or any type of energy without taking in opinion specific elemental defense) is attack energy to energy-needed-to-broke-object ratio multiplied to full health; When the health of object is equal to count of joules which required to destroy the object, the damage calculation is very simple.

---------------------------------

```
damage = (attackEnergy * (1 - defence)) - energyAbsorption;
```
--------------------------------

Where: 
- **attackEnergy** is quantity in jouls of impact
- **defence** - the value of 0 to 1, that reduces the impact energy even up to complete invulnerability
- **energyAbsorption** - A quantity of energy that can be totally absorbed without any damage

 I store the damage in hp units (not in percents or by reducing health). Thus there're two properties: `hp` and `damage`. When `damage` became equal to `hp` the object's destroying (or dying if it is creature).

 I don't use percentage health because there can be a case when a creature (or object) some way increases its health while taking damage. For example, some magic creature that grows up additional head or alien mutant that increases its mass.

## Energy absorption

Little more about _energy absorption_. Some materials can be totally invulnerable to small quantities of impact energy. For example, a mountain doesn't worry about it being attacked by mosquitos, even if there're sextillion of them. The energy of a mosquito's attack probably is about 1 joule, so if an asteroid will not have energy absorption, it is enough 2,000,000,000,000 mosquitos to destroy it. Do you believe it? I'm not sure that it will be enough 2,000,000,000,000 baseball bat's impacts to destroy an asteroid.

The baseball bat impact record is about 2500 kgs, that is 24517 joule (1 kgs = 9.807 joules, right?). So 2,000,000,000,000 baseball bats provide 4.9034e+16 joule of energy. But you can't break an asteroid by basketball bat, even if you hit it 2,000,000,000,000 times.

So, by the default _energyAbsorption_ is 0.1 percent from _hp_;

## The excess energy of destruction and explosion strength

Health describes how much energy an object can take before broke. When an object gets damaged it loses its health.

FatalDamage is the quantity of impact energy that equals to rest health

```
isFatalDamage = damage >= health
```

But if an object takes more damage than needed to destroy it, this remained energy is called excess energy and added to its explosion. Thus physical body must memoize the excess energy to pass to the explosion module.

```
excessEnergy = (damage - health) / hp
```

An explosion module take the excess energy ratio to the object's hp and depends on it calculates the explosion strength.

```
explosionModule.excessEnergyRatio = excessEnergy / hp;
```

So if `excessEnergyRatio` is zero, it provides the weakest explosion. But if `excessEnergyRatio` is one, it gives really nice enough explosion, and if it's _3000_ it will be a great plasmic explosion with a strong shock wave.

## Bullets impact energy and collision's explosions

Since bullet energy can not be applied to defender size (because defender size is unable to affect bullet's impact explosion size). The primary role in forming a bullet's impact explosion size has bullet size. But in arcade games bullet's size is redundant. Thus the explosions of collisions won't be calculated relative _power_ property, but instead, it takes in the opinion impact _energy_.

