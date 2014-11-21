﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//DYNAMO
using Autodesk.DesignScript.Geometry;
using Autodesk.DesignScript.Runtime;

//SAP 
using SAP2000v16;
using SAPConnection;
using DynamoSAP.Assembly;
using DynamoSAP.Structure;

namespace DynamoSAP.Analysis
{
    public class Analysis : IResults
    {
        public List<FrameResults> FrameResults { get; set; }
        

        private static cSapModel mySapModel;

        public static StructuralModel Run(StructuralModel Model, string Filepath, bool RunIt)
        {
            if (RunIt)
            {
                // open sap     
                SAPConnection.Initialize.OpenSAPModel(Filepath, ref mySapModel);
                // run analysis
                SAPConnection.AnalysisMapper.RunAnalysis(ref mySapModel, Filepath);
            }
            return Model;
        }

        public static Analysis GetResults(StructuralModel Model, string LoadCase, bool Run)
        {
            List<FrameResults> frameResults = null;
            Analysis StructureResults = new Analysis();
            if (Run)
            {
                // loop over frames get results and populate to dictionary
                frameResults = SAPConnection.AnalysisMapper.GetFrameForces(ref mySapModel, LoadCase);
                StructureResults.FrameResults = frameResults;
            }
            return StructureResults;
        }

        public static List<double> DecomposeResults(Analysis AnalysisResults, string ForceType, string loadcase, int FrameID)
        {

            FrameID -= 1; // SAP starts numbering elements by 1, but the first dictionary in the list is in index 0

            List<double> Forces = new List<double>();
            foreach (FrameAnalysisData fad in AnalysisResults.FrameResults[FrameID].Results[loadcase].Values)
            {
                if (ForceType == "Axial") //Get Axial Forces P
                {
                    Forces.Add(fad.P);
                }
                else if (ForceType == "Shear22") // Get Shear V2
                {
                    Forces.Add(fad.V2);
                }
                else if (ForceType == "Shear33") // Get Shear V3
                {
                    Forces.Add(fad.V3);
                }

                else if (ForceType == "Torsion") // Get Torsion T
                {
                    Forces.Add(fad.T);
                }

                else if (ForceType == "Moment22") // Get Moment M2
                {
                    Forces.Add(fad.M2);
                }
                else if (ForceType == "Moment33") // Get Moment M3
                {
                    Forces.Add(fad.M3);
                }
            }

            return Forces;
        }

        public static List<Mesh> VisualizeResults(StructuralModel Model, Analysis AnalysisResults, string ForceType, string loadcase, int FrameID, double scale)
        {
            List<Mesh> myVizMeshes = new List<Mesh>();

            //foreach (int id in FrameIDs)
            //{
                // get the frame's curve specified by the frameID
            int fid = FrameID - 1; // SAP starts numbering elements by 1, but the first dictionary in the list is in index 0
                Frame f = (Frame)Model.Frames[fid];
                Curve c = f.BaseCrv;

                //CREATE LOCAL COORDINATE SYSTEM
                Vector xAxis = c.TangentAtParameter(0.0);
                Vector yAxis = c.NormalAtParameter(0.0);
                //This ensures the right axis for the Z direction  
                CoordinateSystem localCS = CoordinateSystem.ByOriginVectors(c.StartPoint, xAxis, yAxis);

                //TEST TO VISUALIZE NORMALS
                //Point pt = c.PointAtParameter(0.5);
                //Line ln = Line.ByStartPointDirectionLength(pt, localCS.ZAxis, 30.0);
                //myLines.Add(ln);

                
                List<Point> MeshPoints = new List<Point>();

                int count = 0;

                
                double t2=0.0;
                double t1 = 0.0;
                foreach (double t in AnalysisResults.FrameResults[fid].Results[loadcase].Keys)
                {
                    Mesh m = null;
                    IndexGroup ig = null;

                    count += 1;

                    Point cPoint = c.PointAtParameter(t); // curve Point
                    Point vPoint = null; // value Point

                    double translateCoord = 0.0;

                    if (ForceType == "Axial") // Get Axial P
                    {
                        translateCoord = AnalysisResults.FrameResults[fid].Results[loadcase][t].P * -scale;
                    }

                    else if (ForceType == "Shear22") // Get Shear V2
                    {
                        translateCoord = AnalysisResults.FrameResults[fid].Results[loadcase][t].V2 * -scale;
                    }
                    else if (ForceType == "Shear33") // Get Shear V3
                    {
                        translateCoord = AnalysisResults.FrameResults[fid].Results[loadcase][t].V3 * scale;
                    }

                    else if (ForceType == "Torsion") // Get Torsion T
                    {
                        translateCoord = AnalysisResults.FrameResults[fid].Results[loadcase][t].T * scale;
                    }

                    else if (ForceType == "Moment22") // Get Moment M2
                    {
                        translateCoord = AnalysisResults.FrameResults[fid].Results[loadcase][t].M2 * scale;
                    }

                    else if (ForceType == "Moment33") // Get Moment M3
                    {
                        translateCoord = AnalysisResults.FrameResults[fid].Results[loadcase][t].M3 * scale;
                    }

                    
                    double d2 = 0.0;
                    double d1 = 0.0;
                    double pZ = 0.0;

                    if (ForceType == "Moment22")
                    {
                        vPoint = (Point)cPoint.Translate(localCS.YAxis, translateCoord); // Translate in the Y direction to match the visualization of SAP
                        if (MeshPoints.Count > 0)
                        {
                            d2 = vPoint.Y;
                            d1 = MeshPoints[MeshPoints.Count - 1].Y;
                            pZ = cPoint.Y;
                        }
                    }
                    else
                    {
                        vPoint = (Point)cPoint.Translate(localCS.ZAxis, translateCoord); // All the other types must be translate in the Z direction} 
                        if (MeshPoints.Count > 0)
                        {
                            d2 = vPoint.Z;
                            d1 = MeshPoints[MeshPoints.Count - 1].Z;

                            pZ = cPoint.Z;// Z value of the point being visualized
                        }
                    }

                    Point pzero = null;
                    if (MeshPoints.Count == 0)
                    {
                        MeshPoints.Add(cPoint); //index 0
                        MeshPoints.Add(vPoint); //index 1

                    }
                       
                    else// if a previous point has been added
                    {
                        if (count != AnalysisResults.FrameResults[fid].Results[loadcase].Keys.Count) // if it's not the end of the list
                        {
                            List<IndexGroup> indices = new List<IndexGroup>();

                            double tzero;//parameter at which the value of the forces = pZ
                            
                            t2 = t*c.Length; // current t parameter of the point being visualized
                            if ((d1 > pZ && d2 < pZ) || (d1 < pZ && d2 > pZ)) // if there is a change in the force sign, calculate the intersection point
                            {
                                
                                // the function of the line is
                                //y= (t2-t1)tzero/(d2-d1)+d1  This has to be equal to pZ
                                double ml = (d2 - d1)/ (t2 - t1) ;
                                tzero = (pZ - d1) / ml; // multiply by the length of the curve and add the X coordinate of the last mesh point
                                
                                
                                tzero += t1;

                                //pzero= Point.ByCartesianCoordinates(CoordinateSystem.Identity(), tzero, cPoint.Y, pZ); //CHECK THAT THIS IS CORRECT

                                pzero = Point.ByCartesianCoordinates(localCS, tzero, 0.0, 0.0); //CHECK THAT THIS IS CORRECT
                                MeshPoints.Add(pzero); //index 2 

                                ig = IndexGroup.ByIndices(0, 1, 2);
                                indices.Add(ig);
                                // Color coding here

                            }
                            else
                            {
                                MeshPoints.Add(vPoint); //index 2 (note: vPoint before cPoint)
                                MeshPoints.Add(cPoint); //index 3 
                                if (MeshPoints.Count == 3)
                                {
                                    ig = IndexGroup.ByIndices(0, 1, 2);
                                    indices.Add(ig);
                                }
                                else
                                {
                                    ig = IndexGroup.ByIndices(0, 1, 2, 3);
                                    indices.Add(ig);
                                }
                                //color coding here
                            }

                            // Add face
                            //append...??
                            m = Mesh.ByPointsFaceIndices(MeshPoints, indices);
                            myVizMeshes.Add(m);

                            MeshPoints.Clear();
                            
                            if ((d1 > pZ && d2 < pZ) || (d1 < pZ && d2 > pZ)) // if there is a change in the force sign
                            {
                                MeshPoints.Add(pzero); //new face index 0
                            }
                            else
                            {
                                MeshPoints.Add(cPoint); //new face index 0
                                MeshPoints.Add(vPoint); //new face index 1   
                            }
                        }
                        else
                        {
                            MeshPoints.Add(vPoint); //index 2 (note: vPoint before cPoint)
                            MeshPoints.Add(cPoint); //index 3 

                            // Add face
                            List<IndexGroup> indices = new List<IndexGroup>();
                            if (MeshPoints.Count == 3)
                            {
                                ig = IndexGroup.ByIndices(0, 1, 2);
                                indices.Add(ig);
                            }
                            else
                            {
                                ig = IndexGroup.ByIndices(0, 1, 2, 3);
                                indices.Add(ig);
                            }
                            //append...??
                            m = Mesh.ByPointsFaceIndices(MeshPoints, indices);
                            myVizMeshes.Add(m);
                        }
                    }
                    t1 = t*c.Length;
                }
            //}

            return myVizMeshes;

        }

        //Results private methods
        private Analysis() { }
        private Analysis(List<FrameResults> fresults)
        {
            FrameResults = fresults;

        }



    }


}
