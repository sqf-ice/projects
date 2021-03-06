﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml.Linq;
using ZZero.ZPlanner;
using ZZero.ZPlanner.Data;
using ZZero.ZPlanner.Data.Entities;
using ZZero.ZPlanner.UI;
using ZZero.ZPlanner.UI.Dialogs;
using ZZero.ZPlanner.Utils;
using ZZero.ZPlanner.ZConfiguration;
using System.Linq;
using System.Collections.Specialized;

//design units = MILIMETER, MICRON, INCH
namespace ZZero.ZPlanner.Translators
{
    class cProperty
    {
        public string type = ""; //by the 1st attribute: text, value, layerOrGroupRef
        public double value = 0;
        public string text = "";
        public string unit = "";    //MM,INCH,MICRON,OHMS,MHO/CM,SIEMENS/M,SELCIUS,FARENHEIT,PRECENT,Hz,DEGREES,
                                    //RMAX,RZ,RMS,SECTION,CLASS,ITEM,GAUGE,OTHER
        public double tolPlus = 0;
        public double tolMinus = 0;
        public bool   tolPrecent = false;
        public string refUnit = ""; //MM,INCH,MICRON,OHMS,MHO/CM,SIEMENS/M,SELCIUS,FARENHEIT,PRECENT,Hz,DEGREES,
                                    //RMAX,RZ,RMS,SECTION,CLASS,ITEM,GAUGE,OTHER
        public double refValue = 0;
        public string refText = "";
        public string layerOrGroupRef = "";
        public string comment = "";

        public cProperty()
        {
            type = ""; //by the 1st attribute: text, value, layerOrGroupRef
            value = 0;
            text = "";
            unit = "";    //MM,INCH,MICRON,OHMS,MHO/CM,SIEMENS/M,SELCIUS,FARENHEIT,PRECENT,Hz,DEGREES,
                                        //RMAX,RZ,RMS,SECTION,CLASS,ITEM,GAUGE,OTHER
            tolPlus = 0;
            tolMinus = 0;
            tolPrecent = false;
            refUnit = ""; //MM,INCH,MICRON,OHMS,MHO/CM,SIEMENS/M,SELCIUS,FARENHEIT,PRECENT,Hz,DEGREES,
                                        //RMAX,RZ,RMS,SECTION,CLASS,ITEM,GAUGE,OTHER
            refValue = 0;
            refText = "";
            layerOrGroupRef = "";
            comment = "";
        }
        cProperty(cProperty other)
        {
            type = other.type;
            value = other.value;
            text = other.text;
            unit = other.unit;
            tolPlus = other.tolPlus;
            tolMinus = other.tolMinus;
            tolPrecent = other.tolPrecent;
            refUnit = other.refUnit;
            refValue = other.refValue;
            refText = other.refText;
            layerOrGroupRef = other.layerOrGroupRef;
            comment = other.comment;
        }
    }

    class cGroup
    {
        public string name; // General, Conductor, Dielectric, Impedance, Compliance, Technology, Tempretaure, Tool, V-Cut 
        public string type = ""; // enum = f(name)
        public string structure = ""; //for impedance - STRIPLINE, PLANE_LESS_STRIPLINE, MICROSTRIP_EMBEDDED, 
                                    //MICROSTRIP_NO_MASK, MICROSTRIP_MASK_COVERED, MICROSTRIP_DUAL_MASK_COVERED, 
                                    //COPLANAR_WAVEGUIDE_STRIPLINE,COPLANAR_WAVEGUIDE_EMBEDDED, COPLANAR_WAVEGUIDE_NO_MASK, 
                                    //COPLANAR_WAVEGUIDE_MASK_COVERED, COPLANAR_WAVEGUIDE_DUAL_MASK_COVERED
        public string transmission = ""; //for impedance - SINGLE_ENDED, EDGE_COUPLED, BROADSIDE_COUPLED, OTHER
        public string comment = "";
        public List<cProperty> props = null;//new List<cProperty>();

        public cGroup()
        {
            name = ""; // General, Conductor, Dielectric, Impedance, Compliance, Technology, Tempretaure, Tool, V-Cut 
            type = ""; // enum = f(name)
            structure = ""; //for impedance - STRIPLINE, PLANE_LESS_STRIPLINE, MICROSTRIP_EMBEDDED, 
                                        //MICROSTRIP_NO_MASK, MICROSTRIP_MASK_COVERED, MICROSTRIP_DUAL_MASK_COVERED, 
                                        //COPLANAR_WAVEGUIDE_STRIPLINE,COPLANAR_WAVEGUIDE_EMBEDDED, COPLANAR_WAVEGUIDE_NO_MASK, 
                                        //COPLANAR_WAVEGUIDE_MASK_COVERED, COPLANAR_WAVEGUIDE_DUAL_MASK_COVERED
            transmission = ""; //for impedance - SINGLE_ENDED, EDGE_COUPLED, BROADSIDE_COUPLED, OTHER
            comment = "";
            props = new List<cProperty>();
        }

        public cGroup(cGroup other)
        {
            name = other.name;
            type = other.type;
            structure = other.structure;
            transmission = other.transmission;
            comment = other.comment;
            props = new List<cProperty>(other.props);
        }
    }

    class cSpec
    {
        public string name;
        public List<cGroup> groups = null;//new List<cGroup>();

        public cSpec()
        {
            name = "";
            groups = new List<cGroup>();
        }
        public cSpec(cSpec other)
        {
            name = other.name;
            groups = new List<cGroup>(other.groups);
        }
    }

    class cLayer
    {
        public string name = "";
        public cSpec spec = null;
        public double thickness = 0;
        public string layerFunction = "";
    }
//
    class IPC_Parser : IImport
    {
        private string ipcNamespace = "http://webstds.ipc.org/2581";
        private string InputFile;

// file data
        string name = ""; //if not set in the stackup then take it from file name
        string units = "";
        double totalThickness = 0;

        private List<cSpec> specs = new List<cSpec>();
        
        private cSpec specData = null;      //current spec
        private cGroup groupData = null;    //current group
        private cProperty propData = null;  //current prop

        private List<cLayer> layers = new List<cLayer>();
        private cLayer layerData = null;
        private bool bPressedThickness;
        private ZLibraryCategory[] libraryPriorityArray;

        public IPC_Parser(string file, bool bPressed = true, ZLibraryCategory[] libraryPriority = null)
        {
            InputFile = file;
            bPressedThickness = bPressed;
            libraryPriorityArray = libraryPriority;
        }

        public bool Convert()
        {
            ZPlannerManager.StatusMenu.UpdateProgress();
            return true;
        }

        public bool IsValidFile()
        {
            return (Path.GetExtension(InputFile).ToUpper() == ".XML" && File.Exists(InputFile));
        }

        public bool Import()
        {
            ZPlannerManager.StatusMenu.UpdateProgress();
            //turn off event handlers
            ZPlannerManager.SuspendFSHandlers();

            XDocument xDoc = XDocument.Load(InputFile);

            ZPlannerManager.StatusMenu.UpdateProgress();

            Parse(xDoc);

            ZPlannerManager.StatusMenu.UpdateProgress();

            CreateStackup();

            //turn on event handlers
            ZPlannerManager.ResumeFSHandlers();
            ZPlannerManager.Project.Stackup.Singles.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            ZPlannerManager.Project.Stackup.Pairs.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            ZPlannerManager.Project.Stackup.Layers.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            return true;
        }

        private void CreateStackup()
        {
            //ZPlannerManager.Project = new ZPlannerProject();

            ZPlannerProject project = new ZPlannerProject();

            // create stackup
            ZStackup stackup = new ZStackup(project);
            stackup.Title = Path.GetFileNameWithoutExtension(InputFile);

            project.Parameters.Clear();
            project.SubParameters.Clear();

            foreach (SelectList defaultSelectList in project.GetDefaultSelectLists())
            {
                project.Lists.Add(defaultSelectList);
            }

            foreach (ZParameter parameter in project.GetDefaultParameters())
            {
                if (parameter.ID == ZStringConstants.ParameterIDFoilTreatment)
                    parameter.IsHidden = false;

                project.Parameters.Add(parameter);
                if (parameter.SubParameters != null && parameter.SubParameters.Count > 0) project.SubParameters.AddRange(parameter.SubParameters);
            }

            int iLastParameter = project.Parameters.Count - 1;
            int order = project.Parameters[iLastParameter].Order;

            stackup.ActivePair = stackup.Pairs[0];

            project.Stackup = stackup;
            ZPlannerManager.Project = project;

            // parse file
            InitStackup(stackup);

            //remove ActiveSingle
            if (stackup.Singles.Count > 1)
            {
                stackup.Singles.RemoveAt(0);
                stackup.ActiveSingle = stackup.Singles[0];
            }

            //remove ActivePair
            if (stackup.Pairs.Count > 1)
            {
                stackup.Pairs.RemoveAt(0);
                stackup.ActivePair = stackup.Pairs[0];
            }

            SortImpedances(stackup);
            UpdateUsed(stackup);

            if (bPressedThickness)
            {
                stackup.IsKeepImportedPressedThickness = true;
            }
            //CopperThicknessDialog dlg = new CopperThicknessDialog(stackup);
            //dlg.ShowDialog();
            ////if (dlg.DialogResult == DialogResult.OK) ;
            ZPlannerManager.Project.Stackup.CopperCoverageType = ZCopperCoverageType.ManuallyEntered;

            //ui
            ZPlannerManager.StackupPanel = new ZPlannerStackupPanel(ZPlannerManager.Project.Stackup);
            ZPlannerManager.IsSequentialLamination = ZPlannerManager.IsSequentialLaminationCanBeEnabled();
        }

        void UpdateUsed(ZStackup stackup)
        {
            /*ParameterIDZo_IsUsed = "Zo_IsUsed";
        public const string ParameterIDZdiff_IsUsed = "Zdiff_IsUsed";
            */
            foreach (ZSingle single in stackup.Singles)
            {
                foreach (ZLayer singleLayer in single.Layers)
                {
                    switch (singleLayer.GetLayerType())
                    {
                        case ZLayerType.Signal:
                        case ZLayerType.SplitMixed:
                            string Zo = singleLayer.GetLayerParameterValue(ZStringConstants.ParameterIDZo_Zo);
                            if (!string.IsNullOrEmpty(Zo))
                            {
                                singleLayer.SetLayerParameterValue(ZStringConstants.ParameterIDZo_IsUsed, "true");
                            }
                            else
                            {
                                singleLayer.SetLayerParameterValue(ZStringConstants.ParameterIDZo_IsUsed, "false");
                            }
                            break;
                    }
                }
            }

            foreach (ZPair pair in stackup.Pairs)
            {
                foreach (ZLayer pairLayer in pair.Layers)
                {
                    switch (pairLayer.GetLayerType())
                    {
                        case ZLayerType.Signal:
                        case ZLayerType.SplitMixed:
                            //ZLayer baseLayer = stackup.GetLayerOfStackup(pairLayer.ID);
                            //string aname = baseLayer.GetLayerParameterValue(ZStringConstants.ParameterIDLayerName);
                            string Zo = pairLayer.GetLayerParameterValue(ZStringConstants.ParameterIDZdiff_Zdiff);
                            if (!string.IsNullOrEmpty(Zo))
                            {
                                pairLayer.SetLayerParameterValue(ZStringConstants.ParameterIDZdiff_IsUsed, "true");
                            }
                            else
                            {
                                pairLayer.SetLayerParameterValue(ZStringConstants.ParameterIDZdiff_IsUsed, "false");
                            }
                            break;
                    }
                }
            }

        }


        private void SortImpedances(ZStackup stackup)
        {
            char[] sep = { '~', ' ' };
            foreach (ZSingle s in stackup.Singles)
            {
                string[] x = s.Title.Split(sep);
                x = x.Where(w => !string.IsNullOrEmpty(w)).ToArray();

                double z;
                if ((x.Length == 3) && Double.TryParse(x[1], out z)){
                    s.ImpedanceTarget = z;
                }
            }
            foreach (ZPair s in stackup.Pairs)
            {
                string[] x = s.Title.Split(sep);
                x = x.Where(w => !string.IsNullOrEmpty(w)).ToArray();

                double z;
                if ((x.Length == 3) && Double.TryParse(x[1], out z)){
                    s.ImpedanceTarget = z;
                }
            }

            stackup.Singles.Sort((x, y) => x.ImpedanceTarget.CompareTo(y.ImpedanceTarget));
            stackup.Pairs.Sort((x, y) => x.ImpedanceTarget.CompareTo(y.ImpedanceTarget));
        }

        void InitStackup(ZStackup stackup)
        {
            //units
            double lenCoef = 1; 
            switch (units) //MILIMETER, MICRON, INCH
            {
                case "MILIMETER": lenCoef = Units.MilimeterToMils;  break;
                case "MICRON": lenCoef = Units.MicronToMils; break;
                case "INCH": lenCoef = Units.InchToMils;  break;
            }

            foreach (cLayer lay in layers)
            {
                //parameters maps
                Dictionary<string, Dictionary<string, string>> allParMap = new Dictionary<string, Dictionary<string, string>>();
                Dictionary<string, string> layerParMap = new Dictionary<string, string>();
                allParMap.Add("layer", layerParMap);

                //type
                ZLayerType zType = ZLayerType.Signal;
                switch (lay.layerFunction)
                {  //SOLDERMASK, SIGNAL, PLANE, DIELCORE, DIELPREG, others are ignored
                    case "SOLDERMASK": zType = ZLayerType.SolderMask; break;
                    case "SIGNAL": zType = ZLayerType.Signal; break;
                    case "PLANE": zType = ZLayerType.Plane; break;
                    case "MIXED": zType = ZLayerType.SplitMixed; break;
                    case "DIELCORE": zType = ZLayerType.Core; break;
                    case "DIELPREG": zType = ZLayerType.Prepreg; break;
                    default: break; //throw exception..
                }

                layerParMap.Add(ZStringConstants.ParameterIDLayerName, lay.name);
                double th = lay.thickness * lenCoef;

                if (zType == ZLayerType.Prepreg)
                {
                    if (bPressedThickness)
                    {
                        layerParMap.Add(ZStringConstants.ParameterIDPrepregThickness, th.ToString());
                        layerParMap.Add(ZStringConstants.ParameterIDFabricatorThickness, th.ToString());
                    }
                    else
                    {
                        layerParMap.Add(ZStringConstants.ParameterIDOriginThickness, th.ToString());
                        layerParMap.Add(ZStringConstants.ParameterIDThickness, th.ToString());
                    }
                }
                else
                {
                    layerParMap.Add(ZStringConstants.ParameterIDThickness, th.ToString());
                }
                //params from spec
                if (lay.spec != null)//just in case spec wasn't defined
                {
                    foreach (cGroup group in lay.spec.groups)
                    {
                        //  name; // General, Conductor, Dielectric, Impedance, Compliance, Technology, Tempretaure, Tool, V-Cut 
                        switch (group.name)
                        {
                            case "General":
                                if (group.type == "MATERIAL")
                                {
                                    string description = "";
                                    foreach (cProperty prop in group.props)
                                    {
                                        if (prop.type == "text")
                                        {
                                            if (description.Length > 0) description += ",";
                                            description += prop.text;
                                        }
                                    }
                                    //parMap.Add(ZStringConstants.ParameterIDDescription, description);
                                    layerParMap.Add(ZStringConstants.ParameterIDMaterial, description);
                                }
                                else if (group.type == "OTHER" && group.comment == "COMMENTS")
                                {
                                    cProperty p = group.props[0];
                                    if (p.text.Length > 0)
                                    {
                                        layerParMap.Add(ZStringConstants.ParameterIDComments, p.text);
                                    }
                                }
                                break;
                            case "Conductor":  //CONDUCTIVITY,SURFACE_ROUGHNESS_UPFACING, SURFACE_ROUGHNESS_DOWNFACING,
                                //SURFACE_ROUGHNESS_TREATED, ETCH_FACTOR, FINISHED_HEIGHT, OTHER
                                switch (group.type)
                                {
                                    case "CONDUCTIVITY":
                                        {
                                            cProperty p = group.props[0];
                                            if (p.type == "value")
                                            {
                                                double coef = 1;
                                                if (p.unit.Length > 0)
                                                {
                                                    switch (p.unit)
                                                    {
                                                        case "MHO/CM": coef = 1; break;
                                                        case "SIEMENS/M": coef = 0.01; break; //1 MHO/CM = 100 S/M
                                                    }
                                                }
                                                double v = p.value * coef;
                                                //? do we need this value?
                                                //--parMap.Add(ZStringConstants.ParameterIDThermalConductivity, v.ToString());
                                            }
                                        }
                                        break;
                                    case "SURFACE_ROUGHNESS_UPFACING":
                                    case "SURFACE_ROUGHNESS_DOWNFACING":
                                        {
                                            cProperty p = group.props[0];
                                            if (p.type == "value")
                                            {
                                                double coef = 1;
                                                if (p.unit.Length > 0)
                                                {
                                                    switch (p.unit)
                                                    {
                                                        //MM,INCH,MICRO - convert to Microns
                                                        case "INCH": coef = 1000*Units.MilToMicrons; break;
                                                        case "MM": coef = 1000; break;
                                                        case "MICRO": coef = 1; break;
                                                    }
                                                }
                                                if (p.refUnit.Length > 0)
                                                {
                                                    switch (p.refUnit)
                                                    {//RMAX,RZ,RMS
                                                        case "RMAX":
                                                        case "RMS":
                                                        case "RZ": break;
                                                    }
                                                }
                                                double v = p.value * coef;
                                                if (group.type == "SURFACE_ROUGHNESS_UPFACING")
                                                {
                                                    layerParMap.Add(ZStringConstants.ParameterIDRoughTop, v.ToString());
                                                }
                                                else
                                                {
                                                    layerParMap.Add(ZStringConstants.ParameterIDRoughBottom, v.ToString());
                                                }
                                            }
                                        }
                                        break;
                                    case "SURFACE_ROUGHNESS_TREATED": break;
                                    case "ETCH_FACTOR":
                                        {
                                            cProperty p = group.props[0];
                                            if (p.type == "value")
                                            {
                                                layerParMap.Add(ZStringConstants.ParameterIDEtchFactor, p.value.ToString());
                                            }
                                        }
                                        break;
                                    case "FINISHED_HEIGHT": break;
                                    case "OTHER":
                                        switch (group.comment)
                                        {
                                            case "COPPER_PERCENT":
                                            {
                                                cProperty p = group.props[0];
                                                layerParMap.Add(ZStringConstants.ParameterIDCopperPercent, p.value.ToString());
                                            }
                                            break;
                                            case "COPPER_FOIL":
                                            {
                                                cProperty p = group.props[0];
                                                layerParMap.Add(ZStringConstants.ParameterIDFoilTreatment, ZPlannerManager.GetCopperFoilString(p.text));
                                            }
                                            break;
                                            case "COPPER_WEIGHT":
                                            {
                                                cProperty p = group.props[0];
                                                layerParMap.Add(ZStringConstants.ParameterIDCopperThickness, p.text);
                                            }
                                            break;
                                        }
                                        break;
                                }

                                break;
                            case "Dielectric": //DIELECTRIC_CONSTANT,LOSS_TANGENT,GLASS_TYPE,GLASS_STYLE,RESIN_CONTENT,
                                //PROCESSABILITY_TEMPERATURE,OTHER
                                switch (group.type)
                                {
                                    case "OTHER":
                                        switch (group.comment)
                                        {
                                            case "THICKNESS":
                                                cProperty p = group.props[0];
                                                layerParMap.Add(ZStringConstants.ParameterIDOriginThickness, (p.value * Units.InchToMils).ToString());
                                                break;
                                        }
                                        break;
                                    case "DIELECTRIC_CONSTANT":
                                        {
                                            cProperty p1 = group.props[0];
                                            if (group.comment.Length == 0)
                                            {
                                                //we currently have 1 value
                                                layerParMap.Add(ZStringConstants.ParameterIDDielectricConstant, p1.value.ToString());
                                            }
                                            else //single-ended or diff Dk
                                            {
                                                string impName = group.comment;
                                                Dictionary<string, string> impMap = null;
                                                if (allParMap.ContainsKey(impName))
                                                {
                                                    impMap = allParMap[impName];
                                                }
                                                else
                                                {
                                                    impMap = new Dictionary<string, string>();
                                                    allParMap.Add(impName, impMap);
                                                }
                                                if (impName.Contains("Zdiff"))
                                                {
                                                    impMap.Add(ZStringConstants.ParameterIDZdiff_DielectricConstant, p1.value.ToString());
                                                }
                                                else
                                                {
                                                    impMap.Add(ZStringConstants.ParameterIDZo_DielectricConstant, p1.value.ToString());
                                                }
                                            }
                                            
                                        }
                                        break;
                                    case "LOSS_TANGENT":
                                        {
                                            cProperty p1 = group.props[0];
                                            if (group.comment.Length == 0)
                                            {
                                                //we currently have 1 value
                                                layerParMap.Add(ZStringConstants.ParameterIDLossTangent, p1.value.ToString());
                                            }
                                            else
                                            {
                                                string impName = group.comment;
                                                Dictionary<string, string> impMap = null;
                                                if (allParMap.ContainsKey(impName))
                                                {
                                                    impMap = allParMap[impName];
                                                }
                                                else
                                                {
                                                    impMap = new Dictionary<string, string>();
                                                    allParMap.Add(impName, impMap);
                                                }
                                                if (impName.Contains("Zdiff"))
                                                {
                                                    impMap.Add(ZStringConstants.ParameterIDZdiff_LossTangent, p1.value.ToString());
                                                }
                                                else
                                                {
                                                    impMap.Add(ZStringConstants.ParameterIDZo_LossTangent, p1.value.ToString());
                                                }
                                            }
                                        }
                                        break;
                                    case "GLASS_TYPE": break;
                                    case "GLASS_STYLE":
                                        {
                                            cProperty p1 = group.props[0];
                                            layerParMap.Add(ZStringConstants.ParameterIDConstruction, p1.text);
                                        }
                                        break;
                                    case "RESIN_CONTENT":
                                        {
                                            cProperty p1 = group.props[0];
                                            layerParMap.Add(ZStringConstants.ParameterIDResinContent, p1.value.ToString());
                                        } 
                                        break;
                                    case "PROCESSABILITY_TEMPERATURE": break;
                                }
                                break;
                            case "Impedance":  //IMPEDANCE,LINEWIDTH,SPACING,REF_PLANE_LAYER_ID,OTHER 
                                //structure - STRIPLINE, PLANE_LESS_STRIPLINE, MICROSTRIP_EMBEDDED, 
                                //MICROSTRIP_NO_MASK, MICROSTRIP_MASK_COVERED, MICROSTRIP_DUAL_MASK_COVERED, 
                                //COPLANAR_WAVEGUIDE_STRIPLINE,COPLANAR_WAVEGUIDE_EMBEDDED, COPLANAR_WAVEGUIDE_NO_MASK, 
                                //COPLANAR_WAVEGUIDE_MASK_COVERED, COPLANAR_WAVEGUIDE_DUAL_MASK_COVERED
                                //transmission - SINGLE_ENDED, EDGE_COUPLED, BROADSIDE_COUPLED, OTHER
                                string impGroup = group.comment;
                                Dictionary<string, string> impParMap = null;
                                if (allParMap.ContainsKey(impGroup))
                                {
                                    impParMap = allParMap[impGroup];
                                }
                                else {
                                    impParMap = new Dictionary<string, string>();
                                    allParMap.Add(impGroup, impParMap);
                                }
                                switch (group.transmission)
                                {
                                    case "SINGLE_ENDED":
                                    case "EDGE_COUPLED":
                                        switch (group.type)
                                        {
                                            case "IMPEDANCE":
                                                {
                                                    cProperty p1 = group.props[0];//in Ohms
                                                    if (group.transmission == "SINGLE_ENDED")
                                                    {
                                                        impParMap.Add(ZStringConstants.ParameterIDZo_Zo, p1.value.ToString());
                                                    }
                                                    else if (group.transmission == "EDGE_COUPLED")
                                                    {
                                                        impParMap.Add(ZStringConstants.ParameterIDZdiff_Zdiff, p1.value.ToString());
                                                    }
                                                } break;
                                            case "LINEWIDTH":
                                                {
                                                    cProperty p = group.props[0];
                                                    if (p.type == "value")
                                                    {
                                                        double coef = 1;
                                                        if (p.unit.Length > 0)
                                                        {
                                                            switch (p.unit)
                                                            {
                                                                //MM,INCH,MICRO
                                                                case "INCH": coef = Units.InchToMils; break;
                                                                case "MM": coef = Units.MilimeterToMils; break;
                                                                case "MICRO": coef = Units.MicronToMils; break;
                                                            }
                                                        }
                                                        double v = p.value * coef;
                                                        if (group.transmission == "SINGLE_ENDED")
                                                        {
                                                            impParMap.Add(ZStringConstants.ParameterIDZo_TraceWidth, v.ToString());
                                                        }
                                                        else if (group.transmission == "EDGE_COUPLED")
                                                        {
                                                            impParMap.Add(ZStringConstants.ParameterIDZdiff_TraceWidth, v.ToString());
                                                        }
                                                    }
                                                }
                                                break;
                                            case "SPACING":
                                                {
                                                    cProperty p = group.props[0];
                                                    if (p.type == "value")
                                                    {
                                                        double coef = 1;
                                                        if (p.unit.Length > 0)
                                                        {
                                                            switch (p.unit)
                                                            {
                                                                //MM,INCH,MICRO
                                                                case "INCH": coef = Units.InchToMils; break;
                                                                case "MM": coef = Units.MilimeterToMils; break;
                                                                case "MICRO": coef = Units.MicronToMils; break;
                                                            }
                                                        }
                                                        double v = p.value * coef;
                                                        if (group.transmission == "EDGE_COUPLED")
                                                        {
                                                            impParMap.Add(ZStringConstants.ParameterIDZdiff_TraceSpacing, v.ToString());
                                                        }
                                                    }
                                                }
                                                break;
                                            case "REF_PLANE_LAYER_ID":
                                                cProperty pRef1 = group.props[0];
                                                cProperty pRef2 = group.props[1];

                                                string ref1 = pRef1.text;
                                                string ref2 = pRef2.text;
                                                if (ref1.Length > 0) {
                                                    if (group.transmission == "SINGLE_ENDED")
                                                    {
                                                        impParMap.Add(ZStringConstants.ParameterIDZo_TopReference, ref1);
                                                    }
                                                    else if (group.transmission == "EDGE_COUPLED")
                                                    {
                                                        impParMap.Add(ZStringConstants.ParameterIDZdiff_TopReference, ref1);
                                                    }
                                                }
                                                if (ref2.Length > 0)
                                                {
                                                    if (group.transmission == "SINGLE_ENDED")
                                                    {
                                                        impParMap.Add(ZStringConstants.ParameterIDZo_BottomReference, ref2);
                                                    }
                                                    else if (group.transmission == "EDGE_COUPLED")
                                                    {
                                                        impParMap.Add(ZStringConstants.ParameterIDZdiff_BottomReference, ref2);
                                                    }
                                                }
                                                break;
                                        }
                                        break;
                                }
                                break;
                        }
                    }
                }

                // create layer
                string layerID = stackup.AddLayer(zType);
                ZLayer zLayer = stackup.GetLayerOfStackup(layerID);

                foreach (KeyValuePair<string, Dictionary<string,string>> kvp in allParMap)
                {
                    string key = kvp.Key;
                    Dictionary<string, string> parMap = kvp.Value;

                    if (key == "layer")
                    {
                        foreach (KeyValuePair<string, string> kvp1 in parMap)
                        {
                            ZLayerParameter layerParameter = zLayer.GetLayerParameter(kvp1.Key);
                            if (layerParameter != null) layerParameter.SetEditedValue(kvp1.Value);
                        }
                    }
                    else if (key.Contains("Zdiff"))
                    {
                        ZPair pair = stackup.Pairs.Find(x => x.Title == key);
                        if (pair == null)
                        {
                            pair = new ZPair(stackup);
                            pair.Title = key;
                            stackup.Pairs.Add(pair);
                        }

                        ZLayer zDiffLayer = pair.GetLayerOfPairImpedance(layerID);
                        foreach (KeyValuePair<string, string> kvp1 in parMap)
                        {
                            ZLayerParameter layerParameter = zDiffLayer.GetLayerParameter(kvp1.Key);
                            if (layerParameter != null) layerParameter.SetEditedValue(kvp1.Value);
                        }
                    }
                    else if (key.Contains("Zo"))
                    {
                        ZSingle single = stackup.Singles.Find(x => x.Title == key);
                        if (single == null)
                        {
                            single = new ZSingle(stackup);
                            single.Title = key;
                            stackup.Singles.Add(single);
                        }

                        ZLayer zSingleLayer = single.GetLayerOfSingleImpedance(layerID);
                        foreach (KeyValuePair<string, string> kvp1 in parMap)
                        {
                            ZLayerParameter layerParameter = zSingleLayer.GetLayerParameter(kvp1.Key);
                            if (layerParameter != null) layerParameter.SetEditedValue(kvp1.Value);
                        }

                    }
                }
            }
        }

        //Parse functions
        private void Parse(XDocument xDoc)
        {
            foreach (XElement x in xDoc.Root.Elements())
            {
                switch (x.Name.LocalName ){
                    case "Ecad": ParseEcad(x);
                            break;
                }
            }
        }

        private void ParseEcad(XElement ecad)
        {
            foreach (XElement x in ecad.Elements())
            {
                switch (x.Name.LocalName)
                {
                    case "CadHeader": ParseCadHeader(x); break;
                    case "CadData": ParseCadData(x); break;
                }
            }
        }

        //----------------------------------------------------------------
        private void ParseCadHeader(XElement cadHeader)
        {
            //cad header attributes
            XAttribute a = cadHeader.Attribute("units");
            if (a != null)
            {
                units = a.Value;
            }

            //spec list
            IEnumerable<XElement> elems = cadHeader.Elements();
            foreach (XElement y in elems)
            {
                if (y.Name.LocalName == "Spec")
                {
                    specData = new cSpec();
                    ParseSpec(y);
                    specs.Add(specData);
                }
            }
        }

        private void ParseSpec(XElement spec)
        {
            //spec attributes
            XAttribute a = spec.Attribute("name");
            if (a!=null) specData.name = a.Value;

            //spec property groups
            IEnumerable<XElement> groups = spec.Elements();
            foreach (XElement z in groups)
            {
                groupData = new cGroup();
                ParseGroup(z);
                specData.groups.Add(groupData);                
            }
        }

        private void ParseGroup(XElement group)
        {
            switch (group.Name.LocalName)
            {
                case "General":    ParseGenericGroup(group, "General");  break;
                case "Dielectric": ParseGenericGroup(group, "Dielectric"); break;
                case "Conductor":  ParseGenericGroup(group, "Conductor"); break;
                case "Impedance":  ParseGenericGroup(group, "Impedance"); break;

                case "Compliance":
                case "Tecnology":
                case "Temperature": break; //skip for now
            }

        }

        private void ParseGenericGroup(XElement group, string groupName)
        {
            groupData.name = groupName;
            //attributes
            XAttribute a = group.Attribute("type");
            if (a!=null) groupData.type = a.Value;
            switch (groupName)
            {
                case "Impedance":
                    a = group.Attribute("structure");
                    if (a != null) groupData.structure = a.Value;
                    a = group.Attribute("transmission");
                    if (a != null) groupData.transmission = a.Value;
                    a = group.Attribute("comment");
                    if (a != null) groupData.comment = a.Value;
                    break;
                case "General":
                    a = group.Attribute("comment");
                    if (a != null) groupData.comment = a.Value;
                    break;
                case "Conductor":
                    a = group.Attribute("comment");
                    if (a != null) groupData.comment = a.Value;
                    break;
                case "Dielectric":
                    a = group.Attribute("comment");
                    if (a != null) groupData.comment = a.Value;
                    break;
            }
            //properties
            IEnumerable<XElement> props = group.Elements();
            foreach (XElement z in props)
            {
                if (z.Name.LocalName == "Property")
                {
                    propData = new cProperty();
                    ParseProperty(z);
                    groupData.props.Add(propData);
                }
            }
        }

        private void ParseProperty(XElement prop)
        {
            //attributes
            XAttribute a = prop.FirstAttribute;
            propData.type = a.Name.LocalName;
            //IEnumerable<XAttribute> attrs = prop.Attributes();

            a = prop.Attribute("text");
            if (a != null) propData.text = a.Value;
            a = prop.Attribute("value");
            if (a != null) Double.TryParse(a.Value, out propData.value);
            a = prop.Attribute("unit");
            if (a != null) propData.unit = a.Value;
            a = prop.Attribute("refUnit");
            if (a != null) propData.refUnit = a.Value;
            a = prop.Attribute("tolPercent");
            if (a != null) propData.tolPrecent = a.Value == "TRUE" ? true : false;
            a = prop.Attribute("minusTol");
            if (a != null) Double.TryParse(a.Value, out propData.tolMinus);
            a = prop.Attribute("plusTol");
            if (a != null) Double.TryParse(a.Value, out propData.tolPlus);
            a = prop.Attribute("layerOrGroupRef");
            if (a != null) propData.layerOrGroupRef = a.Value;
        }

        //-----------------------------------------------------------
        private void ParseCadData(XElement cadData)
        {
            //Layer list
            //Stackup
            //Step(s!)
            IEnumerable<XElement> props = cadData.Elements();
            foreach (XElement z in props)
            {
                switch (z.Name.LocalName) {
                    case "Layer":
                        layerData = new cLayer();
                        ParseLayer(z);
                        layers.Add(layerData);
                        break;
                    case "Stackup": ParseStackup(z); break;
                    //-- case "Step": ParseStep(z); break; //? do we need this data?
                }
            }
        }

        private void ParseLayer(XElement layer)
        {
            //attributes
            XAttribute a = layer.Attribute("name");
            if (a != null) layerData.name = a.Value;
            //--a = layer.Attribute("polarity");
            //--a = layer.Attribute("side");
            //-- a = layer.Attribute("span");
            a = layer.Attribute("layerFunction"); //we get layer type from here
            //SOLDERMASK, SIGNAL, PLANE, DIELCORE, DIELPREG, many others are ignored
            if (a != null) layerData.layerFunction = a.Value;
        }

        private void ParseStackup(XElement stackup)
        {
            //attributes
            XAttribute a = stackup.Attribute("name");
            if (a != null) name = a.Value;
            //--a = stackup.Attribute("whereMeasured");
            //--a = stackup.Attribute("tolMinus");
            //--a = stackup.Attribute("tolPlus");
            a = stackup.Attribute("overallThickness");//!total thickness
            if (a != null) Double.TryParse(a.Value, out totalThickness);

            //get stackup group
            XElement group = null;
            IEnumerable<XElement> elems = stackup.Elements();
            foreach (XElement d in elems)
            {
                if (d.Name.LocalName == "StackupGroup")
                {
                    group = d;
                    Debug.WriteLine(d.Name.LocalName);
                    break;
                }
            }

            //XElement group = stackup.Element("StackupGroup");//just the first group
            a = group.Attribute("name");
            //--a = group.Attribute("tolMinus");
            //--a = group.Attribute("tolPlus");
            //--a = group.Attribute("thickness"); 

            //analyze group
            IEnumerable<XElement> lays = group.Elements();//"StackupLayer");
            foreach (XElement l in lays)
            {
                a = l.Attribute("sequence");
                int seqId = -1;
                if (a != null)
                {
                    Int32.TryParse(a.Value, out seqId);
                    if (seqId < 2 || seqId > layers.Count) seqId = -1; //valid ids start with 2
                }

                a = l.Attribute("layerOrGroupRef");
                if (a != null)
                {
                    //find layer with that name in layers list
                    string layer_name = a.Value;
                    int idxLay = layers.FindIndex(x => x.name == layer_name);
                    if (idxLay < 0 && seqId > 0)//if not matched by name use number
                    {
                        idxLay = seqId - 2;
                    }
                    if (idxLay >= 0)
                    {
                        //--a = l.Attribute("tolMinus");
                        //--a = l.Attribute("tolPlus");
                        a = l.Attribute("thickness");//! we get layer thickness from here
                        double thick = 0;
                        if (a != null && Double.TryParse(a.Value, out thick)){
                            layers[idxLay].thickness = thick;
                        }
                        //spec reference
                        IEnumerable<XElement> es = l.Elements();
                        XElement spec = null;
                        foreach (XElement e in es)
                        {
                            if (e.Name.LocalName == "SpecRef")
                            {
                                spec = e;
                                break;
                            }
                        }
                        //--XElement spec = l.Elements();// ("SpecRef");
                        a = spec.Attribute("id");//link layer to spec
                        if (a != null)
                        {
                            string spec_name = a.Value;
                            //find spec in specs list
                            int idxSpec = specs.FindIndex(x => x.name == spec_name);
                            if (idxSpec >= 0)
                            {
                                layers[idxLay].spec = specs[idxSpec];
                            }
                            else //if not matched by name - find spec by layer number
                            {
                                if (seqId > 0) layers[idxLay].spec = specs[seqId - 2];
                            }
                        }
                    }
                }
            }
        }

        private void ParseStep(XElement step)
        {
            //attributes
            XAttribute a = step.Attribute("name");
            //a.Value

            //properties
            IEnumerable<XElement> props = step.Elements();
            foreach (XElement z in props)
            {
                if (z.Name.LocalName == "LayerFeature")
                {
                    a = z.Attribute("layerRef"); 
                    //a.value
                }
            }
        }
    }

    class IPC_Exporter
    {
        private ZStackup stackup;
        private string OutputFile;
        List<string> layNames;

        public IPC_Exporter(string file)
        {
            OutputFile = file;
        }

        public void Export()
        {
            stackup = ZPlannerManager.Project.Stackup;
            if (stackup == null) return;

            layNames = new List<string>();

            ExportIPC(stackup);
        }

        public void ExportIPC(ZStackup stackup)
        {
            File.WriteAllText(OutputFile, CreateText());
        }

        private string CreateText()
        {
            //header
            string s = "<?xml version=\"1.0\"?>" + Environment.NewLine;
            s += "<IPC-2581 xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://webstds.ipc.org/2581\" revision=\"B\">" + Environment.NewLine;
            // content header
            s += ContentHeader();

            // logistic header
            s += LogisticHeader();

            //history record
            s += HistoryRecord();

            //ecad
            s += "<Ecad name=\"Design\">" + Environment.NewLine;
            s += CadHeader();
            s += CadData();
            s += "</Ecad>" + Environment.NewLine;
            // close header
            s += "</IPC-2581>" + Environment.NewLine;
            return s;
        }
        //
        private string ContentHeader()
        {
            stackup = ZPlannerManager.Project.Stackup;

            string s = "<Content roleRef=\"Owner\">" + Environment.NewLine;
            s += "<FunctionMode level=\"1\" mode=\"USERDEF\"/>" + Environment.NewLine;
            s += "<StepRef name=" + "\"" + stackup.Title + "\"/>" + Environment.NewLine;
            int layNum = stackup.Layers.Count;
            int iMetal = 0;
            int iDiel = 0;
            int iLay = -1;

            HashSet<string> uniqueLayerNames = new HashSet<string>();

            foreach (ZLayer zl in stackup.Layers)
            {
                iLay++;
                string name = zl.GetLayerParameterValue(ZStringConstants.ParameterIDLayerName);
                bool bNotUnique = uniqueLayerNames.Contains(name);

                if (name.Length == 0)//use generic name
                {
                    string prefix = "Layer_";
                    switch (zl.GetLayerType())
                    {
                        case ZLayerType.Core:
                        case ZLayerType.Prepreg:
                            iDiel++;
                            prefix = "DIELECTRIC_";
                            name = prefix + iDiel.ToString();
                            break;
                        case ZLayerType.SolderMask:
                            prefix = "SOLDERMASK_";
                            string n = iLay == 0 ? "TOP" : "BOTTOM";
                            name = prefix + n;
                            break;
                        case ZLayerType.Signal:
                        case ZLayerType.SplitMixed:
                            iMetal++;
                            prefix = "SIGNAL_";
                            name = prefix + iMetal.ToString();
                            break;
                        case ZLayerType.Plane:
                            iMetal++;
                            prefix = "PLANE_";
                            name = prefix + iMetal.ToString();
                            break;
                    }
                }
                if (bNotUnique)
                {
                    //find unique name
                    bool b = true;
                    int i = 0;
                    while (b)
                    {
                        i++;
                        string tryName = String.Format("{0}_{1}",name,i);
                        if (!uniqueLayerNames.Contains(tryName))
                        {
                            name = tryName;
                            b = false;
                            break;
                        }
                    }
                }
                uniqueLayerNames.Add(name);

                layNames.Add(name);
                s += "<LayerRef name=" + "\"" + name + "\"/>" + Environment.NewLine;
            }
            s += "</Content>" + Environment.NewLine;
            return s;
        }

        private string LogisticHeader()
        {
            string s = "<LogisticHeader>" + Environment.NewLine;
            s += "<Role roleFunction=\"SENDER\" id=\"Owner\"/>" + Environment.NewLine;
            s += "<Enterprise id=\"UNKNOWN\" code=\"UNKNOWN\"/>" + Environment.NewLine;
            s += "<Person roleRef=\"Owner\" name=\"UNKNOWN\" enterpriseRef=\"UNKNOWN\"/>" + Environment.NewLine;
            s += "</LogisticHeader>" + Environment.NewLine;
            return s;
        }
        
        private string HistoryRecord()
        {
            /*  <HistoryRecord number="1.0" origination="2014-12-05T10:00:20" software="allegro_16.6S037(9/29/2014)" lastChange="2014-12-05T10:00:18" externalConfigurationEntryPoint="unknown">
                <FileRevision fileRevisionId="2581 rev B" comment="first pass at Rev b" label="2581-rev-B-0.1">
                  <SoftwarePackage name="Allegro" revision="allegro_16.6S037(9/29/2014)" vendor="Cadence">
                    <Certification certificationStatus="CERTIFIED"/>
                    </SoftwarePackage>
                  </FileRevision>
                </HistoryRecord>
            */
            string currentDateTime = DateTime.Now.ToString("yyyy-MM-dd") + "T" + DateTime.Now.ToString("HH:mm:ss");

            string s = "<HistoryRecord number=\"1.0\" origination=\"" + currentDateTime +"\" software=\"ZPlanner\" lastChange=\"" + currentDateTime +"\">" + Environment.NewLine;
            s += "<FileRevision fileRevisionId=\"2581 rev B\">" + Environment.NewLine;
            s += "</FileRevision>" + Environment.NewLine;
            s += "</HistoryRecord>" + Environment.NewLine;
            return s;
        }

        private string AirSpec()
        {
            string s = "<Spec name=\"SPEC_LAYER_SURFACE_INDEX_1\">" +  Environment.NewLine +
                       "<General type=\"MATERIAL\">"  + Environment.NewLine +
                        "<Property text=\"AIR\"/>"  + Environment.NewLine +
                        "</General>"  + Environment.NewLine +
                        "<General type=\"OTHER\" comment=\"LAYER_TYPE\">"  + Environment.NewLine +
                        "<Property text=\"SURFACE\"/>"  + Environment.NewLine +
                        "</General>"  + Environment.NewLine +
                        "<Dielectric type=\"DIELECTRIC_CONSTANT\">"  + Environment.NewLine +
                        "<Property value=\"1.000\"/>"  + Environment.NewLine +
                        "</Dielectric>"  + Environment.NewLine +
                        "<Dielectric type=\"LOSS_TANGENT\">"  + Environment.NewLine +
                        "<Property value=\"0.0\"/>"  + Environment.NewLine +
                        "</Dielectric>"  + Environment.NewLine +
                        "</Spec>"  + Environment.NewLine;
            return s;
        }

        private string CadHeader()
        {
            string s = "<CadHeader units=\"INCH\">" + Environment.NewLine;
            int iLay = -1;
            s += AirSpec();
            foreach (ZLayer zl in stackup.Layers)
            {
                iLay++;
                s += Spec(zl, iLay);
            }
            s += "</CadHeader>" + Environment.NewLine;
            return s;
        }

        private string Structure(ZLayer zl, int iLay)
        {
            //STRIPLINE, PLANE_LESS_STRIPLINE, MICROSTRIP_EMBEDDED, 
            //MICROSTRIP_NO_MASK, MICROSTRIP_MASK_COVERED, MICROSTRIP_DUAL_MASK_COVERED, 
            //COPLANAR_WAVEGUIDE_STRIPLINE,COPLANAR_WAVEGUIDE_EMBEDDED, COPLANAR_WAVEGUIDE_NO_MASK, 
            //COPLANAR_WAVEGUIDE_MASK_COVERED, COPLANAR_WAVEGUIDE_DUAL_MASK_COVERED
            string retval = "";
            ZLayerType? type = zl.GetLayerType();
            switch (type)
            {
                case ZLayerType.Core: 
                case ZLayerType.Prepreg:
                case ZLayerType.SolderMask:
                case ZLayerType.Plane:
                    return retval;
            }

            //
            ZStackup stk = zl.Stackup;
            // outer uncovered
            if (iLay == 0 || iLay == stk.Layers.Count - 1)
            {
                return "MICROSTRIP_NO_MASK";
            }

            //outer covered
            if (iLay == 1)
            {
                ZLayer lNext = stk.Layers[0];
                if (lNext.GetLayerType() == ZLayerType.SolderMask)
                {
                    return "MICROSTRIP_MASK_COVERED";
                }
                else
                {
                    return "MICROSTRIP_NO_MASK";
                }
            }

            if (iLay == stk.Layers.Count - 2)
            {
                ZLayer lPrev = stk.Layers[stk.Layers.Count - 1];
                if (lPrev.GetLayerType() == ZLayerType.SolderMask)
                {
                    return "MICROSTRIP_MASK_COVERED";
                }
                else
                {
                    return "MICROSTRIP_NO_MASK";
                }
            }

            // check for refernce layers 
            bool bRefUp = false, bRefDown = false;
            for (int i = 0; i < iLay; i++ ){
                if (stk.Layers[i].isMetal())
                {
                    if (stk.Layers[i].GetLayerType() == ZLayerType.Plane)
                    {
                        bRefUp = true;
                        break;
                    }
                    /*
                    string s = "";
                    if (ZPlannerProject.GetLayerParameterValue(zl, ZStringConstants.ParameterIDPlaneReference, ref s))
                    {
                        if (s == ZStringConstants.PlaneReference)
                        {
                            bRefUp = true;
                            break;
                        }
                    }
                    */
                }
            }
            for (int i = iLay + 1; i < stk.Layers.Count; i++)
            {
                if (stk.Layers[i].isMetal())
                {
                    if (stk.Layers[i].GetLayerType() == ZLayerType.Plane)
                    {
                        bRefDown = true;
                        break;
                    }

                    /*
                    string s = "";
                    if (ZPlannerProject.GetLayerParameterValue(zl, ZStringConstants.ParameterIDPlaneReference, ref s))
                    {
                        if (s == ZStringConstants.PlaneReference)
                        {
                            bRefDown = true;
                            break;
                        }
                    }
                    */
                }
            }

            if (bRefUp && bRefDown)
            {
                return "STRIPLINE";
            }
            else
            {
                return "MICROSTRIP_EMBEDDED";
            }
        }

        private string Spec(ZLayer zl, int iLay)
        {
            string materialName = zl.GetLayerParameterValue(ZStringConstants.ParameterIDMaterial);

            string name = "SPEC_" + layNames[iLay];
            string s = "<Spec name=\"" + name + "\">" + Environment.NewLine;

        //----- General layer properties ------
            string dielType = "";
            switch (zl.GetLayerType())
            {
                case ZLayerType.Core:
                    dielType = "Core";
                    break;
                case ZLayerType.Prepreg:
                    dielType = "Prepreg";
                    break;
                case ZLayerType.SolderMask:
                    dielType = "Mask";
                    break;
            }
            s += "<General type=\"MATERIAL\">" + Environment.NewLine;
            s += "<Property text=\"" + materialName + "\"/>" + Environment.NewLine;
            s += "</General>" + Environment.NewLine;

            s += "<General type=\"OTHER\" comment=\"" + "LAYER_TYPE\">" + Environment.NewLine;
            if (zl.isMetal())
            {
                s += "<Property text=\"Copper\"/>" + Environment.NewLine;
            }
            else
            {
                s += "<Property text=\"Dielectric\"/>" + Environment.NewLine;
                s += "<Property text=\"" + dielType + "\"/>" + Environment.NewLine;
            }
            s += "</General>" + Environment.NewLine;

            string comments = zl.GetLayerParameterValue(ZStringConstants.ParameterIDComments);
            if (comments.Length > 0)
            {
                s += "<General type=\"OTHER\" comment=\"" + "COMMENTS\">" + Environment.NewLine;
                s += "<Property text=\"" + comments + "\"/>" + Environment.NewLine;
                s += "</General>" + Environment.NewLine;
            }

            switch (zl.GetLayerType())
            {
                case ZLayerType.Core:
                case ZLayerType.Prepreg:
                case ZLayerType.SolderMask:
                    //common Dk,Df
                    string dk = zl.GetLayerParameterValue(ZStringConstants.ParameterIDDielectricConstant);
                    double dk_dbl = 0;
                    ZPlannerManager.GetFirstValueFromTable(dk, out dk_dbl);

                    string df = zl.GetLayerParameterValue(ZStringConstants.ParameterIDLossTangent);
                    double df_dbl = 0;
                    ZPlannerManager.GetFirstValueFromTable(df, out df_dbl);

                    double t = 0;
                    Double.TryParse(zl.GetLayerParameterValue(ZStringConstants.ParameterIDOriginThickness), out t);

                    s += "<Dielectric type=\"DIELECTRIC_CONSTANT\">" + Environment.NewLine;
                    s += "<Property value=\"" + dk_dbl.ToString() + "\"/>" + Environment.NewLine;
                    s += "</Dielectric>" + Environment.NewLine;

                    s += "<Dielectric type=\"LOSS_TANGENT\">" + Environment.NewLine;
                    s += "<Property value=\"" + df_dbl.ToString() + "\"/>" + Environment.NewLine;
                    s += "</Dielectric>" + Environment.NewLine;

                    if (zl.GetLayerType() != ZLayerType.SolderMask)
                    {
                        string construction = zl.GetLayerParameterValue(ZStringConstants.ParameterIDConstruction);
                        string resin = zl.GetLayerParameterValue(ZStringConstants.ParameterIDResinContent);

                        s += "<Dielectric type=\"GLASS_STYLE\">" + Environment.NewLine;
                        s += "<Property text=\"" + construction + "\"/>" + Environment.NewLine;
                        s += "</Dielectric>" + Environment.NewLine;
                        
                        s += "<Dielectric type=\"RESIN_CONTENT\">" + Environment.NewLine;
                        s += "<Property value=\"" + resin + "\" unit=\"PERCENT\"/>" + Environment.NewLine;
                        s += "</Dielectric>" + Environment.NewLine;

                        s += "<Dielectric type=\"OTHER\" comment=\"THICKNESS\">" + Environment.NewLine;
                        s += "<Property value=\"" + (t*Units.MilsToInch).ToString() + "\" unit=\"INCH\"/>" + Environment.NewLine;
                        s += "</Dielectric>" + Environment.NewLine;
                    }
                    break;
                case ZLayerType.Signal:
                case ZLayerType.Plane:
                case ZLayerType.SplitMixed:
                    //roughness
                    double dVal = 0;
                    string rTop = zl.GetLayerParameterValue(ZStringConstants.ParameterIDRoughTop);
                    if (string.IsNullOrWhiteSpace(rTop)) rTop = zl.GetLayerParameterValue(ZStringConstants.ParameterIDCalcRoughTop);
                    rTop = Double.TryParse(rTop, out dVal) ? dVal.ToString() : "";

                    string rBot = zl.GetLayerParameterValue(ZStringConstants.ParameterIDRoughBottom);
                    if (string.IsNullOrWhiteSpace(rBot)) rBot = zl.GetLayerParameterValue(ZStringConstants.ParameterIDCalcRoughBottom);
                    rBot = Double.TryParse(rBot, out dVal) ? dVal.ToString() : "";

                    s += "<Conductor type=\"SURFACE_ROUGHNESS_UPFACING\">" + Environment.NewLine;
                    s += "<Property value=\"" + rTop + "\" unit=\"MICRON\" refUnit=\"RZ\"/>" + Environment.NewLine;
                    s += "</Conductor>" + Environment.NewLine;
                    s += "<Conductor type=\"SURFACE_ROUGHNESS_DOWNFACING\">" + Environment.NewLine;
                    s += "<Property value=\"" + rBot + "\" unit=\"MICRON\" refUnit=\"RZ\"/>" + Environment.NewLine;
                    s += "</Conductor>" + Environment.NewLine;

                    //copper fill %
                    string fill = zl.GetLayerParameterValue(ZStringConstants.ParameterIDCopperPercent);
                    if (fill.Length > 0)
                    {
                        s += "<Conductor type=\"OTHER\" comment=\"" + "COPPER_PERCENT\">" + Environment.NewLine;
                        s += "<Property value=\"" + fill + "\" unit=\"PERCENT\"/>" + Environment.NewLine;
                        s += "</Conductor>" + Environment.NewLine;
                    }
                    //copper foil
                    string foil = zl.GetLayerParameterValue(ZStringConstants.ParameterIDFoilTreatment);
                    if (foil.Length > 0)
                    {
                        s += "<Conductor type=\"OTHER\" comment=\"" + "COPPER_FOIL\">" + Environment.NewLine;
                        s += "<Property text=\"" + foil + "\"/>" + Environment.NewLine;
                        s += "</Conductor>" + Environment.NewLine;
                    }
                    //copper weight
                    string weight = zl.GetLayerParameterValue(ZStringConstants.ParameterIDCopperThickness);
                    if (weight.Length > 0)
                    {
                        s += "<Conductor type=\"OTHER\" comment=\"" + "COPPER_WEIGHT\">" + Environment.NewLine;
                        s += "<Property text=\"" + weight + "\" unit=\"OTHER\" comment=\"OUNCE\"/>" + Environment.NewLine;
                        s += "</Conductor>" + Environment.NewLine;
                    }
                    break;
            }

            //---- impedance data -----
            
            foreach (ZSingle single in stackup.Singles){
                string title = single.Title;
                ZLayer singleEnded = single.GetLayerOfSingleImpedance(zl.ID);

                bool bUsed = singleEnded.GetLayerParameterValue(ZStringConstants.ParameterIDZo_IsUsed) == "true";

                switch (singleEnded.GetLayerType())
                {
                    case ZLayerType.Core:
                    case ZLayerType.Prepreg:
                    case ZLayerType.SolderMask:
                        //specific Dk(f), Df(f)
                        string dk = singleEnded.GetLayerParameterValue(ZStringConstants.ParameterIDZo_DielectricConstant);
                        double dk_dbl = 0;
                        if (ZPlannerManager.GetFirstValueFromTable(dk, out dk_dbl))
                        {
                            s += "<Dielectric type=\"DIELECTRIC_CONSTANT\"" + " comment=\"" + title + "\">" + Environment.NewLine;
                            s += "<Property value=\"" + dk_dbl.ToString() + "\"/>" + Environment.NewLine;
                            s += "</Dielectric>" + Environment.NewLine;
                        }

                        string df = singleEnded.GetLayerParameterValue(ZStringConstants.ParameterIDZo_LossTangent);
                        double df_dbl = 0;
                        if (ZPlannerManager.GetFirstValueFromTable(df, out df_dbl))
                        {
                            s += "<Dielectric type=\"LOSS_TANGENT\"" + " comment=\"" + title + "\">" + Environment.NewLine;
                            s += "<Property value=\"" + df_dbl.ToString() + "\"/>" + Environment.NewLine;
                            s += "</Dielectric>" + Environment.NewLine;
                        }
                        break;
                    case ZLayerType.Signal:
                    case ZLayerType.SplitMixed:
                        if (bUsed)
                        {
                            double dVal = 0;
                            string wSingle = singleEnded.GetLayerParameterValue(ZStringConstants.ParameterIDZo_TraceWidth);
                            wSingle = Double.TryParse(wSingle, out dVal) ? (dVal * Units.MilsToInch).ToString() : "";

                            string zSingle = singleEnded.GetLayerParameterValue(ZStringConstants.ParameterIDZo_Zo);
                            zSingle = Double.TryParse(zSingle, out dVal) ? dVal.ToString("N2") : "";

                            string ref1 = singleEnded.GetLayerParameterValue(ZStringConstants.ParameterIDZo_TopReference);
                            string ref2 = singleEnded.GetLayerParameterValue(ZStringConstants.ParameterIDZo_BottomReference);

                            string structure = Structure(zl, iLay);

                            s += "<Impedance type=\"IMPEDANCE\" transmission=\"SINGLE_ENDED\" structure=\"" + structure + "\"" + " comment=\"" + title + "\">" + Environment.NewLine;
                            s += "<Property value=\"" + zSingle + "\" unit=\"OHM\"/>" + Environment.NewLine;
                            s += "</Impedance>" + Environment.NewLine;
                            s += "<Impedance type=\"LINEWIDTH\" transmission=\"SINGLE_ENDED\" structure=\"" + structure + "\"" + " comment=\"" + title + "\">" + Environment.NewLine;
                            s += "<Property value=\"" + wSingle + "\" unit=\"INCH\"/>" + Environment.NewLine;
                            s += "</Impedance>" + Environment.NewLine;


                            s += "<Impedance type=\"REF_PLANE_LAYER_ID\" transmission=\"SINGLE_ENDED\" structure=\"" + structure + "\"" + " comment=\"" + title + "\">" + Environment.NewLine;
                            s += "<Property text=\"" + ref1 + "\"/>" + Environment.NewLine;
                            s += "<Property text=\"" + ref2 + "\"/>" + Environment.NewLine;
                            s += "</Impedance>" + Environment.NewLine;
                        }
                        break;
                }
            }//single's layer

            foreach (ZPair pair in stackup.Pairs)
            {
                ZLayer diffPair = pair.GetLayerOfPairImpedance(zl.ID);
                string title = pair.Title;

                bool bUsed = diffPair.GetLayerParameterValue(ZStringConstants.ParameterIDZdiff_IsUsed) == "true";

                switch (zl.GetLayerType())
                {
                    case ZLayerType.Core:
                    case ZLayerType.Prepreg:
                    case ZLayerType.SolderMask:
                        //specific Dk(f), Df(f)
                        string dk = diffPair.GetLayerParameterValue(ZStringConstants.ParameterIDZdiff_DielectricConstant);
                        double dk_dbl = 0;
                        ZPlannerManager.GetFirstValueFromTable(dk, out dk_dbl);

                        string df = diffPair.GetLayerParameterValue(ZStringConstants.ParameterIDZdiff_LossTangent);
                        double df_dbl = 0;
                        ZPlannerManager.GetFirstValueFromTable(df, out df_dbl);

                        s += "<Dielectric type=\"DIELECTRIC_CONSTANT\"" + " comment=\"" + title + "\">" + Environment.NewLine;
                        s += "<Property value=\"" + dk_dbl.ToString() + "\"/>" + Environment.NewLine;
                        s += "</Dielectric>" + Environment.NewLine;

                        s += "<Dielectric type=\"LOSS_TANGENT\"" + " comment=\"" + title + "\">" + Environment.NewLine;
                        s += "<Property value=\"" + df_dbl.ToString() + "\"/>" + Environment.NewLine;
                        s += "</Dielectric>" + Environment.NewLine;
                        break;
                    case ZLayerType.Signal:
                    case ZLayerType.SplitMixed:
                        if (bUsed)
                        {
                            double dVal = 0;

                            string wDiff = diffPair.GetLayerParameterValue(ZStringConstants.ParameterIDZdiff_TraceWidth);
                            wDiff = Double.TryParse(wDiff, out dVal) ? (dVal * Units.MilsToInch).ToString() : "";

                            string sDiff = diffPair.GetLayerParameterValue(ZStringConstants.ParameterIDZdiff_TraceSpacing);
                            sDiff = Double.TryParse(sDiff, out dVal) ? (dVal * Units.MilsToInch).ToString() : "";

                            string zDiff = diffPair.GetLayerParameterValue(ZStringConstants.ParameterIDZdiff_Zdiff);
                            zDiff = Double.TryParse(zDiff, out dVal) ? (dVal).ToString("N2") : "";

                            string ref1 = diffPair.GetLayerParameterValue(ZStringConstants.ParameterIDZdiff_TopReference);
                            string ref2 = diffPair.GetLayerParameterValue(ZStringConstants.ParameterIDZdiff_BottomReference);

                            string structure = Structure(zl, iLay);

                            s += "<Impedance type=\"IMPEDANCE\" transmission=\"EDGE_COUPLED\" structure=\"" + structure + "\"" + " comment=\"" + title + "\">" + Environment.NewLine;
                            s += "<Property value=\"" + zDiff + "\" unit=\"OHM\" tolPercent=\"TRUE\" />" + Environment.NewLine;
                            s += "</Impedance>" + Environment.NewLine;
                            s += "<Impedance type=\"LINEWIDTH\" transmission=\"EDGE_COUPLED\" structure=\"" + structure + "\"" + " comment=\"" + title + "\">" + Environment.NewLine;
                            s += "<Property value=\"" + wDiff + "\" unit=\"INCH\"/>" + Environment.NewLine;
                            s += "</Impedance>" + Environment.NewLine;
                            s += "<Impedance type=\"SPACING\" transmission=\"EDGE_COUPLED\" structure=\"" + structure + "\"" + " comment=\"" + title + "\">" + Environment.NewLine;
                            s += "<Property value=\"" + sDiff + "\" unit=\"INCH\"/>" + Environment.NewLine;
                            s += "</Impedance>" + Environment.NewLine;

                            s += "<Impedance type=\"REF_PLANE_LAYER_ID\" transmission=\"SINGLE_ENDED\" structure=\"" + structure + "\"" + " comment=\"" + title + "\">" + Environment.NewLine;
                            s += "<Property text=\"" + ref1 + "\"/>" + Environment.NewLine;
                            s += "<Property text=\"" + ref2 + "\"/>" + Environment.NewLine;
                            s += "</Impedance>" + Environment.NewLine;
                        }
                        break;
                }
            }//pair's layer


            s += "</Spec>" + Environment.NewLine;
            return s;
        }

        private string CadData()
        {
            string s = "<CadData>" + Environment.NewLine;
            s += LayerList();
            s += Stackup();
            s += Step();
            s += "</CadData>" + Environment.NewLine;
            return s;
        }

        private string LayerList()
        {
            string s = "";
            int iLay = -1;
            int iTop = -1, iBot = -1;
            bool bTop = true;
            foreach (ZLayer zl in stackup.Layers)
            {
                iLay++;
                if (zl.isMetal())
                {
                    iBot = iLay;
                    if (bTop)
                    {
                        bTop = false;
                        iTop = iLay;
                    }
                }
            }
            //
            iLay = -1;
            foreach (ZLayer zl in stackup.Layers)
            {
                iLay++;
                string name = layNames[iLay];
                string layFunction = "";
                switch (zl.GetLayerType())
                {
                    case ZLayerType.Core:
                        layFunction = "DIELCORE";
                        break;
                    case ZLayerType.Prepreg:
                        layFunction = "DIELPREG";
                        break;
                    case ZLayerType.SolderMask:
                        layFunction = "SOLDERMASK";
                        break;
                    case ZLayerType.Signal:
                        layFunction = "SIGNAL";
                        break;
                    case ZLayerType.SplitMixed:
                        layFunction = "MIXED";
                        break;
                    case ZLayerType.Plane:
                        layFunction = "PLANE";
                        break;
                }
                string side = "INTERNAL";
                if (iLay <= iTop){
                    side = "TOP";
                }
                if (iLay >= iBot)//stackup.Layers.Count - 1)
                {
                    side = "BOTTOM";
                }
                s += "<Layer name=\"" + name + "\" polarity=\"POSITIVE\" side=\"" + side + "\" layerFunction=\"" + layFunction + "\"/>" + Environment.NewLine;
            }

            return s;
        }

        private string Stackup()
        {
            string thick = (stackup.GetBoardThickness() * Units.MilsToInch).ToString();
            string s = "<Stackup name=\"" + stackup.Title + "\" whereMeasured=\"METAL\" overallThickness=\"" + thick + "\" tolPlus=\"0.0\" tolMinus=\"0.0\">" + Environment.NewLine;
            //stackup group
            s += "<StackupGroup name=\"AllStackupLayers\" thickness=\"" + thick + "\" tolPlus=\"0.0\" tolMinus=\"0.0\">" + Environment.NewLine;

            //stackup layers
            bool bAddAir = false;
            //top surface layer
            if (bAddAir)
            {
                s += "<StackupLayer layerOrGroupRef=\"SURFACE_INDEX_1\" thickness=\"0.0\"" + " tolPlus=\"0.0\" tolMinus=\"0.0\"" + " sequence=\"1\">" + Environment.NewLine;
                s += "<SpecRef id=\"SPEC_LAYER_SURFACE_INDEX_1\"/>" + Environment.NewLine;
                s += "</StackupLayer>" + Environment.NewLine;
            }
            int iLay = -1;
            int seq = 1;
            foreach (ZLayer zl in stackup.Layers)
            {
                iLay++;
                seq++;//starts from 2
                string name = layNames[iLay];
                double t = 0;
                bool bThick =  false;
                //by default we export pressed prepreg thickness
                if (zl.GetLayerType() == ZLayerType.Prepreg)
                {
                    bThick = Double.TryParse(zl.GetLayerParameterValue(ZStringConstants.ParameterIDPrepregThickness), out t);
                }
                else
                {
                    bThick = Double.TryParse(zl.GetLayerParameterValue(ZStringConstants.ParameterIDThickness), out t);
                }
                string layThick = bThick ? (t * Units.MilsToInch).ToString() : ""; 

                //string layThick = t.ToString();
                s += "<StackupLayer layerOrGroupRef=\"" + name + "\" thickness= \"" + layThick + "\" tolPlus=\"0.0\" tolMinus=\"0.0\"" + " sequence=\"" + seq.ToString() + "\">" + Environment.NewLine;
                s += "<SpecRef id=\"" + "SPEC_" + name + "\"/>" + Environment.NewLine;
                s += "</StackupLayer>" + Environment.NewLine;
            }
            //bottom surface layer
            if (bAddAir)
            {
                s += "<StackupLayer layerOrGroupRef=\"SURFACE_INDEX_2\" thickness=\"0.0\"" + " tolPlus=\"0.0\" tolMinus=\"0.0\"" + " sequence=\"1\">" + Environment.NewLine;
                s += "<SpecRef id=\"SPEC_LAYER_SURFACE_INDEX_1\"/>" + Environment.NewLine;
                s += "</StackupLayer>" + Environment.NewLine;
            }
            s += "</StackupGroup>" + Environment.NewLine;
            s += "</Stackup>" + Environment.NewLine;
            return s;
        }

        string Step()
        {
            string s = "<Step name=\"" + stackup.Title + "\">" + Environment.NewLine;
            //int iLay = -1;
            //foreach (ZLayer zl in stackup.Layers)
            //{
            //    iLay++;
            //    string name = layNames[iLay];
            //    s += "<LayerFeature layerRef=\"" + name +"\"/>" + Environment.NewLine;
            //} 
            s += "</Step>" + Environment.NewLine;
            return s;
        }
    }

 }

/*
IPC 2581 structure:
Header:
- Content
- LogisticHeader
- HistoryRecord
= Ecad
 * CadHeader
    * Spec
        - Compliance type = enum
        - Conductor  type = enum
        - Dielectric type = enum
        - General type = enum
        - Impedance type = enum, transmission = enum, structure = enum
        - Tecnology type = enum
        - Temperature type = enum
        - Tool type = enum ???
        - V-Cut ???
 // any enum includes Other, which requires Comment field
 
        + Property
            - type ???
            - value = double
            - text = string
            - unit = enum
            - tolPlus, tolMinus = nonNegative double
            - tolPercent = TRUE, FALSE
            - refUnit = enum
            - refValue = double
            - refText = string
            - layerOrGroupRef = qualifiedNameType
            - comment = string
 * CadData
    * Layer(s) - name, polarity = enum, side = enum, span, SpecRef, layerFunction = enum
    * Stackup - name, whereMeasured, overallThickness, tolPlus, tolMinus, comment, MatDesType, SpecRef
        * StackupGroup - name, thickness, tolPlus, tolMinus
            * StackupLayer - layerOrGroupRef, sequence, thickness, comment, MatDesType, SpecRef
    * Step - name
        * LayerFeature = layerRef
*/
