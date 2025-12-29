# Crates & Barrels - Stylized Destructibles

This package includes **9 handcrafted stylized props**, each with its own **debris variant**, and a **dust and splinters VFX** to enhance visual feedback during destruction.

All prefabs are **low-poly** and share a **single material**, making them highly **performance-efficient**, especially in scenes with multiple instances.

| Name      | Prop Tri count | Debris Tri count |
| --------- | -------------- | ---------------- |
| crate_01  | 806            | 372              |
| crate_02  | 442            | 464              |
| crate_03  | 886            | 756              |
| crate_04  | 618            | 608              |
| crate_05  | 488            | 536              |
| crate_06  | 640            | 364              |
| barrel_01 | 1,048          | 592              |
| barrel_02 | 1,156          | 872              |
| bucket_01 | 856            | 392              |

## Stylized Lit Shader

All textures are **channel-packed** to match Unity’s standard **Mask Map format**, ensuring full compatibility with the **Lit shaders in both URP and HDRP** right out of the box.

However, for a more polished and stylized visual result, we’ve included a custom **Stylized Lit Shader**. It introduces two key features:

1. **Ground Shadow Gradient**
   Adds a subtle vertical gradient that simulates baked lighting, helping props feel more grounded in the scene.
2. **Vertex Ambient Occlusion**
   Each model includes baked AO stored in **vertex colors**, allowing the shader to create richer shading without extra textures. This approach not only enhances visual depth but also enables **all props to share a single material**, keeping the asset highly modular and performant.

## Destructible Object Component (Sample Scene)

This component turns any `GameObject` into a breakable object that spawns debris when its health reaches zero.

> **Note:** The `GameObject` must have a `Collider` component that can be triggered in order to receive damage.

It has two exposed fields:

-   Health Points `int` – The object's total hit points before it breaks.
-   Debris Prefab `GameObject` – The prefab that will be spawned as debris when the object is destroyed

You can use a `Raycast` to apply damage to a destructible object. For example

```csharp
if (Physics.Raycast(ray, out RaycastHit hit)) {
    var destructible = hit.collider.GetComponent<DestructibleObject>();

    if (destructible != null) {
        destructible.ApplyDamage(10, hit.point);
    }
}
```

A working example is available in the `PlayerController` script.

> ⚠️ If sample scene's interaction doesn't work, enable the **Legacy Input System** in Project Settings.
