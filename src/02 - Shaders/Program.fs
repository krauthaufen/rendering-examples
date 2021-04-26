open FSharp.Data.Adaptive
open Aardvark.Base
open Aardvark.Rendering
open Aardvark.SceneGraph
open Aardvark.Application

// We start by defining a module that holds our shaders.
module Shader =
    open FShade

    // Our vertex-type defines several fields that can be used in shaders.
    // Each field is annotated with a **Semantic** which guides the shader-composition.
    // Note that neither the field-names nor the vertex-types need to match for the composition to work. (see below)
    type Vertex =
        {
            // FShade defines attributes for some well-known **Semantics** (e.g. Position)
            [<Position>]                pos         : V4d
            [<Normal>]                  normal      : V3d
            [<Color>]                   myValue     : V4d

            // For application-specific data we can either use `Semantic(string)` or define a new attribute via inheriting from `SemanticAttribute`
            [<Semantic("ViewNorm")>]    viewNormal  : V3d
            [<Semantic("ViewPos")>]     viewPos     : V4d
        }

    // here's a very simple vertex shader for transforming positions and normals
    // which uses aardvark's builtin transformations `ModelViewTrafo = View * Model` and `ProjTrafo` (both are provided by the `Sg` implementation).
    // NOTE that the normal-transformation here is actually incorrect but for demonstration-purposes it will do.
    let transform (v : Vertex) =
        vertex {
            let viewPos = uniform.ModelViewTrafo * v.pos
            let viewNormal = uniform.ModelViewTrafo * V4d(v.normal, 0.0) |> Vec.xyz

            let projPos = uniform.ProjTrafo * viewPos

            return { v with pos = projPos; viewPos = viewPos; viewNormal = Vec.normalize viewNormal }
        }

    // a simple shader replacing the color with an encoded normal
    let normalColor (v : Vertex) =
        vertex {
            let n = Vec.normalize v.normal
            let c = 0.5 * n + V3d(0.5, 0.5, 0.5)
            return { v with myValue = V4d(c, 1.0) }
        }

    // simple diffuse lighting for a headlight
    let shade (v : Vertex) =
        fragment {
            let vn = Vec.normalize v.viewNormal
            let vp = v.viewPos.XYZ

            // uniforms can be accessed using the `?` operator (or by defining extensions like above for ModelViewTrafo, etc.)
            let a : float = uniform?Ambient

            let diffuse = Vec.dot vn (Vec.normalize vp) |> abs
            return V4d(v.myValue.XYZ * (a + (1.0 - a) * diffuse), v.myValue.W)
        }





[<EntryPoint>]
let main _argv = 
    Aardvark.Init()

    let sg =  
        Sg.ofList [
            // a box with normal-colored faces.
            // Note that the color given to `box'` actually has no effect since the shader won't use it.
            Sg.box' C4b.Red (Box3d(-V3d.III, V3d.III))
            |> Sg.shader {
                do! Shader.transform
                do! Shader.normalColor
                do! Shader.shade
            }

            
            // a red sphere with our custom shading.
            Sg.sphere' 6 C4b.Red 1.0
            |> Sg.translate 0.0 0.0 2.0
            |> Sg.shader {
                do! Shader.transform
                do! Shader.shade
            }

            // a cylinder using a mixture of custom-shaders and default ones.
            Sg.cylinder' 32 C4b.Blue 0.6 2.0
            |> Sg.translate 0.0 2.0 -1.0
            |> Sg.shader {
                do! Shader.transform
                do! DefaultSurfaces.constantColor C4f.White
                do! Shader.shade
            }
        ]
        |> Sg.uniform "Ambient" (AVal.constant 0.2)




    show {
        backend Backend.GL
        initialCamera (CameraView.lookAt (V3d(6,5,4)) V3d.Zero V3d.OOI)
        debug false
        scene sg
    }
    0
