# Unity-Bullet-Hell
An extremely efficient projectile and particle generator for Unity.

## What is it?
![Animated Gif](http://www.jgallant.com/images/unitybullethell/projectiles.gif)

This system makes use of Unity's Graphics.DrawMeshInstancedIndirect functionality, resulting in a single draw call per Projectile Type.  This highly efficient system is able to handle an incredibly large number of projectiles at the same time.

## How do I use this?
This system is composed of a couple of parts.  
#### Projectile Material
![Material Example](http://www.jgallant.com/images/unitybullethell/mats.png)

Two sample Materials are provided, but you can easily create your own to use.  The material you create must use the "ProjectileShader", in order for this system to work.  You must also make sure to select "Enable GPU Instancing", otherwise the GPU instancing will not work.  

If you are not planning on changing the color of each instance of your projectiles, make sure to select "Static Color.  This will stop sending the Color data to the GPU, and increase performance.  If you want to be able to control the color of each individual instance, leave "Static Color" unchecked.

#### Projectile Type
![Projectile Example](http://www.jgallant.com/images/unitybullethell/projectile.png)

Projectile Types are prefabs with an attached "ProjectileType" Monobehaviour derived script.  They are stored in the "/Resources/ProjectileTypes" folder.  These define the Mesh, Material, and MaxCount of the projectile in question.  

When creating new Projectile Types, make sure to use the correct Projectile Material.  You can use any Mesh you want.

You will also want to tweak the "Max Projectile Count" value to be as low as possible for your application, in order to maximize efficiency.  This caps the size of the buffers and data pools.  If it is set too low, you will run out of projectiles to fire.  If set too high, you will have reserved too much memory for your application.

#### Emitters
![Emitter Example](http://www.jgallant.com/images/unitybullethell/emitter.png)

Emitters spawn projectiles into the world, and determine their appearance and how they behave.

There are three Emitter scripts provided:  ProjectileEmitterBasic, ProjectileEmitterAdvanced, ProjectileEmitterShape.  The properties on these scripts can be programmatically controlled, or even animated.  

#### Shape Emitters
![Shape Example](http://www.jgallant.com/images/unitybullethell/shape.png)

You can also create a custom shape template, and place it in "/Resources/ShapeTemplates".  You can then use this Shape Template, on the ProjectileEmitterShape instance, and it will spawn the pattern you specified.

#### One Draw Call
The most amazing part is that you can draw large amounts of meshes in a single draw call.  In the example below, every dot of color is a sphere mesh, and is all done in a single draw call.
http://www.jgallant.com/images/unitybullethell/test.gif
