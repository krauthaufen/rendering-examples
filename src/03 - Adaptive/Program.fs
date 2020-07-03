open FSharp.Data.Adaptive
open Aardvark.Base
open Aardvark.Base.Rendering
open Aardvark.SceneGraph
open Aardvark.Application


[<EntryPoint>]
let main _argv = 
    Aardvark.Init()

    let win =
        window {
            backend Backend.GL
            initialCamera (CameraView.lookAt (V3d(6,5,4)) V3d.Zero V3d.OOI)
            debug false
        }
        
    let time =
        win.Time |> AVal.map (fun t -> 
            float t.Ticks / float System.TimeSpan.TicksPerSecond
        )

    let sg =  
        Sg.ofList [
            let rand = RandomSystem()
            for x in -2 .. 2 do
                for y in -2 .. 2 do
                    let color = 
                        rand.UniformC3f().ToC4b()

                    let speed = 
                        rand.UniformDouble() * Constant.PiHalf

                    let trafo = 
                        time |> AVal.map (fun t -> Trafo3d.RotationZ(speed * t))

                    Sg.box' color (Box3d.FromCenterAndSize(V3d.Zero, V3d(0.4, 0.4, 0.4)))
                    |> Sg.trafo trafo
                    |> Sg.translate (float x) (float y) 0.0
        ]
        |> Sg.shader {
            do! DefaultSurfaces.trafo
            do! DefaultSurfaces.simpleLighting
        }


    win.Scene <- sg
    win.Run()

    0
