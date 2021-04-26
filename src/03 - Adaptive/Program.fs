open System
open FSharp.Data.Adaptive
open Aardvark.Base
open Aardvark.Rendering
open Aardvark.SceneGraph
open Aardvark.Application
open Aardvark.Rendering.Text

let textConfig =
    {
        font = FontSquirrel.Hack.Regular
        color = C4b.White
        align = TextAlignment.Center
        flipViewDependent = true
        renderStyle = RenderStyle.NoBoundary
    }

[<EntryPoint>]
let main _argv = 
    Aardvark.Init()

    let win =
        window {
            backend Backend.GL
            initialCamera (CameraView.lookAt (V3d(6,5,4)) V3d.Zero V3d.OOI)
            debug false
        }
        

    // when SPACE is not held the timeInSeconds shall be relative to
    // some starting-point `t0`
    let timeInSeconds =
        let t0 = DateTime.Now
        win.Keyboard.IsDown Keys.Space |> AVal.bind (function
            | true ->
                AVal.constant 0.0
            | false ->
                win.Time |> AVal.map (fun t ->
                    (t - t0).TotalSeconds
                )
        )
        
    // since the Window might be stereoscopic we get multiple view/proj transformations
    // here, but for this example we choose to ignore all but the first.
    let viewProj = 
        (win.View, win.Proj) ||> AVal.map2(fun view proj -> view.[0] * proj.[0])

    // for the current (adaptive) mouse location calculate the NDC (normalized device coordinate)
    let ndc = 
        (win.Mouse.Position, win.Sizes) ||> AVal.map2 (fun p s ->
            let c = V2d p.Position / V2d s
            let ndc = V2d(2.0 * c.X - 1.0, 1.0 - 2.0 * c.Y)
            ndc
        )

    // get the intersection point with the XY plane or return (0,0,0) when
    // no such intersection exists
    let targetPoint =
        (viewProj, ndc) ||> AVal.map2 (fun viewProj ndc ->
            let p0 = viewProj.Backward.TransformPosProj(V3d(ndc, -1.0))
            let p1 = viewProj.Backward.TransformPosProj(V3d(ndc, 1.0))
            let forward = Vec.normalize (p1 - p0)

            let ray = Ray3d(p0, forward)

            let mutable t = 0.0
            if ray.Intersects(Plane3d.ZPlane, &t) && t >= 0.0 then
                ray.GetPointOnRay t
            else
                V3d.Zero
        )


    let sg =  
        Sg.ofList [
            let rand = RandomSystem()
            for x in -3 .. 3 do
                for y in -3 .. 3 do
                    let speed = 
                        rand.UniformDouble() * Constant.PiHalf

                    let color = 
                        rand.UniformC3f().ToC4b()

                    // orient the coordinate frame s.t. x points to `targetPoint`
                    let orientation = 
                        targetPoint |> AVal.map (fun p ->
                            let x = p - V3d(float x, float y, 0.0) |> Vec.normalize
                            let z = V3d.OOI
                            let y = Vec.cross z x |> Vec.normalize
                            Trafo3d.FromBasis(x, y, z, V3d.Zero)
                        )

                    // rotate around the x-axis
                    let rotation = 
                        timeInSeconds |> AVal.map (fun t -> Trafo3d.RotationX(speed * t))

                    // create cone with r=0.2 and h=0.6 (up=z, z=0 -> bottom)
                    Sg.cone' 16 color 0.2 0.6 

                    // shift the code s.t. z=0 is in its "center"
                    |> Sg.translate 0.0 0.0 -0.3

                    // rotate it s.t. x is the up-direction
                    |> Sg.transform (Trafo3d.RotationY Constant.PiHalf)

                    // rotate it around its x-axis
                    |> Sg.trafo rotation

                    // orient it s.t. it faces the targetPoint
                    |> Sg.trafo orientation

                    // move it to its grid position
                    |> Sg.translate (float x) (float y) 0.0


            // show a text overlay that dynamically rotates
            Sg.textWithConfig textConfig (AVal.constant "Move the Mouse\nto change the target")
            |> Sg.scale 0.5
            |> Sg.transform (Trafo3d.RotationX Constant.PiHalf)
            |> Sg.translate 0.0 0.0 2.0
            |> Sg.trafo (timeInSeconds |> AVal.map (fun t -> Trafo3d.RotationZ(t * 0.1)))
        ]
        |> Sg.shader {
            do! DefaultSurfaces.trafo
            do! DefaultSurfaces.simpleLighting
        }


    win.Scene <- sg
    win.Run()

    0
