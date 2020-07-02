open Aardvark.Base
open Aardvark.Base.Rendering
open Aardvark.SceneGraph
open Aardvark.Application

[<EntryPoint>]
let main _argv = 
    // First we need to initialize aardvark's runtime system. 
    // This ensures that native dependencies and several other things work.
    Aardvark.Init()

    // In the following we define a triangle with per-vertex colors which we'd like to render
    let triangle =  
        // the desired draw-mode is a triangle list here.
        // NOTE that the primitive-count will automatically be determined based on the length of 
        //      the vertex-attributes here, however it can be given explicitly when directly constructing a `Sg.RenderNode`
        Sg.draw IndexedGeometryMode.TriangleList

        // here we apply out vertex-attributes with well-known names (positions, colors)
        // NOTE that we pass float32-positions and byte-colors since the backend does not perform any conversions here.
        //      It is possible to use double-attributes but doing so has drastic performance implications.
        |> Sg.vertexAttribute' DefaultSemantic.Positions [| V3f(-1.0f, -1.0f, 0.0f); V3f(1.0f, -1.0f, 0.0f); V3f(0.0f, 1.0f, 0.0f) |]
        |> Sg.vertexAttribute' DefaultSemantic.Colors [| C4b.Red; C4b.Green; C4b.Blue |]

        // finally we apply the most primitie shaders to the scene.
        |> Sg.shader {
            do! DefaultSurfaces.trafo           // transforms vertices using standard matrices
            do! DefaultSurfaces.vertexColor     // a fragment-shader returning interpolated vertex-colors
        }

    // the simplest way to quickly show a SceneGraph is using the `show` builder.
    // this will include a basic WSAD camera-controller and a little help-text.
    // For a more sophisticated example see the GameWindow example.
    show {
        backend Backend.GL
        debug false
        scene triangle
    }
    0
