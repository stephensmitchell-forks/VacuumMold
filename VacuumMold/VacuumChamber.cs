﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AppKit;
using CoreGraphics;
using Foundation;
using SceneKit;
using static VacuumMold.Helpers;
namespace VacuumMold
{
    [Register ("VacuumChamber")]
    public class VacuumChamber : SCNView
    {
        readonly SCNScene scene = SCNScene.Create ();

        readonly SCNNode moldsNode = SCNNode.Create ();

        readonly SCNNode camNode = new SCNNode {
            Camera = new SCNCamera {
                ZNear = 1,
                ZFar = 10000,
                YFov = 90,
            },
            Position = new SCNVector3 (0, 0, 500),
        };

        public VacuumChamber (CGRect frame)
            : base (frame) => Initialize ();
        public VacuumChamber (NSCoder coder)
            : base (coder) => Initialize ();
        public VacuumChamber (IntPtr handle)
            : base (handle) => Initialize ();
        public VacuumChamber (NSObjectFlag t)
            : base (t) => Initialize ();

        void Initialize ()
        {
            BackgroundColor = NSColor.Black;
            var root = scene.RootNode;
            root.AddChildNode (camNode);
            root.AddChildNode (moldsNode);
            moldsNode.AddChildNode (SCNNode.FromGeometry (SCNSphere.Create (50)));
            Scene = scene;

            UpdateCamera ();
        }

        public override void Layout ()
        {
            base.Layout ();
            UpdateCamera ();
        }

        public void AddMold (Mold mold)
        {
            moldsNode.AddChildNode (mold.Node);
        }

        public void RemoveMold (Mold mold)
        {
        }

        void UpdateCamera ()
        {
            var depth = (double)camNode.Position.Z;
            var bounds = Bounds;
            var height = bounds.Height;
            if (height < 1)
                return;

            var fovyRads = 2.0 * Math.Atan2 (height / 2, depth);
            var fovy = fovyRads * 180.0 / Math.PI;
            camNode.Camera.YFov = fovy;
            camNode.Position = new SCNVector3 (bounds.Width / 2, bounds.Height / 2, (nfloat)depth);
        }
    }
}
