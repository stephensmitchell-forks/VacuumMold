﻿using System;
using System.Numerics;
using System.Linq;
using SceneKit;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Foundation;
using AppKit;

namespace VacuumMold
{
    public class Mold
    {
        private Shape? shape;

        public SCNNode Node { get; }

        public Shape? Shape {
            get => shape;
            set {
                shape = value;
                if (shape != null) {
                    Node.Geometry = CreateGeometry (shape);
                }
            }
        }

        public Vector3 Position {
            get => Node.Position.ToVector3 ();
            set => Node.Position = value.ToSCNVector3 ();
        }

        float pad = 16.0f;
        float depth = 16.0f;

        public Mold ()
        {
            Node = SCNNode.Create ();
        }

        SCNGeometry CreateGeometry (Shape shape)
        {
            //
            // Load the shape
            //
            var shapePoints = shape.SamplePerimeter (1);
            var ipoly = new Poly2Tri.Triangulation.Polygon.Polygon (
                from p in shapePoints
                select new Poly2Tri.Triangulation.Polygon.PolygonPoint (p.X, p.Y));
            var minx = shapePoints.Min (p => p.X);
            var miny = shapePoints.Min (p => p.Y);
            var maxx = shapePoints.Max (p => p.X);
            var maxy = shapePoints.Max (p => p.Y);

            //
            // Tesselate the outer element
            //
            var oframe = new CoreGraphics.CGRect (minx - pad, miny - pad, (maxx - minx) + 2 * pad, (maxy - miny) + 2 * pad);
            var oshapePoints = new Box (oframe).SamplePerimeter(1);
            var opoly = new Poly2Tri.Triangulation.Polygon.Polygon (
                from p in oshapePoints
                select new Poly2Tri.Triangulation.Polygon.PolygonPoint (p.X, p.Y));
            opoly.AddHole (ipoly);

            //
            // Create the outer element
            //
            Poly2Tri.P2T.Triangulate (opoly);
            var opoints = opoly.Triangles.SelectMany (x => x.Points).Distinct ().ToList ();
            var opointToVertex = new Dictionary<uint, ushort> ();
            for (var i = 0; i < opoints.Count; i++) {
                opointToVertex[opoints[i].VertexCode] = (ushort)i;
            }
            var verts = opoints.Select (x => new SCNVector3 ((nfloat)x.X, (nfloat)x.Y, 0)).ToList ();
            var otris = new List<ushort> ();
            foreach (var t in opoly.Triangles) {
                foreach (var p in t.Points) {
                    otris.Add (opointToVertex[p.VertexCode]);
                }
            }
            SCNGeometryElement oelem;
            unsafe {
                var ntris = otris.Count;
                var atris = otris.ToArray ();
                fixed (ushort* ptris = atris) {
                    var oelemData = NSData.FromBytes ((IntPtr)ptris, (nuint)(ntris * 2));
                    oelem = SCNGeometryElement.FromData (oelemData, SCNGeometryPrimitiveType.Triangles, ntris / 3, 2);
                }
            }

            //
            // Create the inner element
            //
            Poly2Tri.P2T.Triangulate (ipoly);
            var ipoints = ipoly.Triangles.SelectMany (x => x.Points).Distinct ().ToList ();
            var ipointToVertex = new Dictionary<uint, ushort> ();
            for (var i = 0; i < ipoints.Count; i++) {
                ipointToVertex[ipoints[i].VertexCode] = (ushort)(i + verts.Count);
            }
            verts.AddRange (ipoints.Select (x => new SCNVector3 ((nfloat)x.X, (nfloat)x.Y, depth)));
            var itris = new List<ushort> ();
            foreach (var t in ipoly.Triangles) {
                foreach (var p in t.Points) {
                    itris.Add (ipointToVertex[p.VertexCode]);
                }
            }
            SCNGeometryElement ielem;
            unsafe {
                var ntris = itris.Count;
                var atris = itris.ToArray ();
                fixed (ushort* ptris = atris) {
                    var ielemData = NSData.FromBytes ((IntPtr)ptris, (nuint)(ntris * 2));
                    ielem = SCNGeometryElement.FromData (ielemData, SCNGeometryPrimitiveType.Triangles, ntris / 3, 2);
                }
            }

            //
            // Create the geometry
            //
            var sources = new[] {
                SCNGeometrySource.FromVertices (verts.ToArray ()),
            };
            var elements = new[] {
                oelem, ielem,
            };
            var g = SCNGeometry.Create (sources, elements.ToArray ());
            var omat = SCNMaterial.Create ();
            omat.Diffuse.ContentColor = NSColor.Red;
            var imat = SCNMaterial.Create ();
            imat.Diffuse.ContentColor = NSColor.Green;
            g.Materials = new[] { omat, imat };
            return g;
        }
    }
}
