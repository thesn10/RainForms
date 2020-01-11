using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using Rainmeter;

namespace PluginRainForms
{
    class Measure
    {
        internal static List<Measure> Measures = new List<Measure>();

        internal Measure Parent;
        internal IntPtr Skin;
        internal string Name;
        internal Type Type;
        internal bool Invalid = false;

        internal API api;
        internal LogHelper log;

        internal Control Control;

        internal string TabName;

        public Measure()
        {
            Measures.Add(this);
        }

        internal void Dispose()
        {
            Measures.Remove(this);
        }

        internal void AddControlToParent()
        {
            Control.ControlCollection ctrlCollection;
            if (Parent.Control is TabControl tabControl)
            {
                if (TabName == null || TabName == "")
                {
                    log.LogError(Name + " requires a TabName to be in a TabControl");
                    Invalid = true;
                    return;
                }

                if (!tabControl.TabPages.ContainsKey(TabName))
                { 
                    tabControl.TabPages.Add(TabName, TabName);
                }

                ctrlCollection = tabControl.TabPages[TabName].Controls;
            }
            else
            {
                ctrlCollection = Parent.Control.Controls;
            }
            ctrlCollection.Add(Control);
        }

        internal void ParseMeasureProps(API api)
        {
            this.api = api;
            this.log = new LogHelper(api);
            this.Skin = api.GetSkin();
            this.Name = api.GetMeasureName();
        }

        static public implicit operator Measure(IntPtr data)
        {
            return (Measure)GCHandle.FromIntPtr(data).Target;
        }
        public IntPtr buffer = IntPtr.Zero;
    }

    internal static class PropertyParser
    {
        internal static void ParseProperties(Rainmeter.API api, object obj)
        {
            LogHelper log = new LogHelper(api);
            Type type = obj.GetType();
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            EventInfo[] events = type.GetEvents(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo pinfo in properties)
            {
                if (pinfo.CanWrite)
                {
                    if (pinfo.PropertyType == typeof(string))
                    {
                        //log.LogDebug("Found string Property: " + pinfo.Name);
                        string propval = api.ReadString(pinfo.Name, "");
                        if (propval != "")
                        {
                            log.LogDebug("Setting " + pinfo.PropertyType.Name + " Property " + pinfo.Name + " to " + propval);
                            pinfo.SetValue(obj, propval, null);
                        }
                    }
                    else if (pinfo.PropertyType == typeof(int))
                    {
                        int val = (int)pinfo.GetValue(obj, null);
                        //log.LogDebug("Found int Property: " + pinfo.Name + " with value: " + val);
                        int propval = api.ReadInt(pinfo.Name, val);
                        if (propval != val)
                        {
                            log.LogDebug("Setting " + pinfo.PropertyType.Name + " Property " + pinfo.Name + " to " + propval);
                            pinfo.SetValue(obj, propval, null);
                        }
                    }
                    else if (pinfo.PropertyType == typeof(bool))
                    {
                        bool val = (bool)pinfo.GetValue(obj, null);
                        //log.LogDebug("Found " + pinfo.PropertyType + " Property: " + pinfo.Name + " with value: " + val);
                        bool propval = api.ReadInt(pinfo.Name, Convert.ToInt32(val)) > 0;
                        if (propval != val)
                        {
                            log.LogDebug("Setting " + pinfo.PropertyType.Name + " Property " + pinfo.Name + " to " + propval);
                            pinfo.SetValue(obj, propval, null);
                        }
                    }
                    else if (pinfo.PropertyType == typeof(float))
                    {
                        float val = (float)pinfo.GetValue(obj, null);
                        //log.LogDebug("Found float Property: " + pinfo.Name + " with value: " + val);
                        float propval = (float)api.ReadDouble(pinfo.Name, val);
                        if (propval != val)
                        {
                            log.LogDebug("Setting " + pinfo.PropertyType.Name + " Property " + pinfo.Name + " to " + propval);
                            pinfo.SetValue(obj, propval, null);
                        }
                    }
                    else if (pinfo.PropertyType == typeof(double))
                    {
                        double val = (double)pinfo.GetValue(obj, null);
                        //log.LogDebug("Found double Property: " + pinfo.Name + " with value: " + val);
                        double propval = api.ReadDouble(pinfo.Name, val);
                        if (propval != val)
                        {
                            log.LogDebug("Setting " + pinfo.PropertyType.Name + " Property " + pinfo.Name + " to " + propval);
                            pinfo.SetValue(obj, propval, null);
                        }
                    }
                    else
                    {
                        string propval = api.ReadString(pinfo.Name, "");
                        if (propval != "")
                        {
                            object val = null;

                            if (pinfo.PropertyType == typeof(Color))
                            {
                                val = Util.ColorFromString(propval);
                            }
                            else
                            {
                                val = pinfo.PropertyType.IsEnum ?
                                    Util.EnumFromString(pinfo.PropertyType, propval) :
                                    Util.ObjectFromString(api, pinfo.PropertyType, propval);
                            }

                            if (val != null)
                            {
                                pinfo.SetValue(obj, val, null);
                            }
                            else
                            {
                                log.LogPropertyValueNotValid(propval, pinfo);
                            }
                        }
                    }
                }
            }

            foreach (EventInfo einfo in events)
            {
                // winforms syntax for events
                string evntval = api.ReadString(einfo.Name, "");
                if (evntval == "")
                {
                    // rainmeter syntax for events
                    evntval = api.ReadString("On" + einfo.Name, "");
                    if (evntval == "")
                    {
                        continue;
                    }
                }

                // remove existing event
                cEventHelper.RemoveEventHandler(obj, einfo.Name);

                // create new event
                EventHandler untypedHandler = delegate (object sender, EventArgs args)
                {
                    api.Execute(evntval);
                };

                Delegate typedHandler = Delegate.CreateDelegate(einfo.EventHandlerType,
                untypedHandler.Target, untypedHandler.Method);
                einfo.AddEventHandler(obj, typedHandler);
            }
        }
    }

    public class Plugin
    {
        [DllExport]
        public static void Initialize(ref IntPtr data, IntPtr rm)
        {
            Measure measure = new Measure();
            data = GCHandle.ToIntPtr(GCHandle.Alloc(measure));
            Rainmeter.API api = (Rainmeter.API)rm;
            measure.ParseMeasureProps(api);
        }

        [DllExport]
        public static void Finalize(IntPtr data)
        {
            Measure measure = (Measure)data;
            if (measure.buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(measure.buffer);
            }
            GCHandle.FromIntPtr(data).Free();

            if (measure.Control != null && !measure.Control.IsDisposed)
            {
                measure.Control.Dispose();
                measure.Control = null;
            }
            measure.Dispose();
        }

        [DllExport]
        public static void Reload(IntPtr data, IntPtr rm, ref double maxValue)
        {
            Measure measure = (Measure)data;
            Rainmeter.API api = (Rainmeter.API)rm;

            string type = api.ReadString("Type","");

            measure.Type = typeof(Control)
                .Assembly
                .GetTypes()
                .Where(x => x.GetBaseTypes().Contains(typeof(Control)))
                .FirstOrDefault(x => x.Name == type);

            if (measure.Type == default(Type))
            {
                measure.log.LogParameterNotValid(type, "Type");
                measure.Invalid = true;
                return;
            }

            if (measure.Control == null || measure.Type.GetType() != measure.Type || measure.Invalid)
            {
                measure.Control = (Control)measure.Type.GetConstructor(Type.EmptyTypes).Invoke(null);
            }

            PropertyParser.ParseProperties(api, measure.Control);

            measure.TabName = api.ReadString("TabName", "");

            if (measure.Control.GetType() != typeof(Form))
            {
                // Find parent using name AND the skin handle to be sure that it's the right one.
                string parentName = api.ReadString("ParentName", "");
                foreach (Measure parentMeasure in Measure.Measures)
                {
                    if (parentMeasure.Skin.Equals(measure.Skin) && parentMeasure.Name.Equals(parentName))
                    {
                        measure.Parent = parentMeasure;
                    }
                }

                if (measure.Parent == null)
                {
                    measure.log.LogError("RainForms.dll: " + measure.Type.ToString() + " needs a parent.");
                    measure.Invalid = true;
                    return;
                }
                measure.AddControlToParent();
            }
        }

        [DllExport]
        public static double Update(IntPtr data)
        {
            Measure measure = (Measure)data;

            if (measure.Invalid) return 0.0d;

            if (measure.Control is CheckBox cb)
            { 
                return Convert.ToDouble(cb.Checked);
            }

            return 0.0d;
        }

        [DllExport]
        public static IntPtr GetString(IntPtr data)
        {
            Measure measure = (Measure)data;

            if (measure.buffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(measure.buffer);
                measure.buffer = IntPtr.Zero;
            }

            if (measure.Invalid)
            {
                measure.buffer = Marshal.StringToHGlobalUni("");
                return measure.buffer;
            }

            string returnval = "";

            if (measure.Control is TextBox tb)
            {
                returnval = tb.Text;
            }

            measure.buffer = Marshal.StringToHGlobalUni(returnval);
            return measure.buffer;
        }

        [DllExport]
        public static void ExecuteBang(IntPtr data, [MarshalAs(UnmanagedType.LPWStr)]String arg)
        {
            Measure measure = (Measure)data;

            if (arg == "" || measure.Invalid) return;

            var args = arg.Split(' ');

            if (args[0] == "RFTypeInfo")
            {
                Type type = Util.TypeFromString(args[1]);
                if (type == null)
                {
                    measure.log.LogError("The type \"" + args[1] + "\" was not found");
                    return;
                }

                measure.log.LogNotice("--------------------------");
                Util.PrintTypeInfo(measure.log, type);
                measure.log.LogNotice("Supported constructors:");
                if (!type.IsEnum)
                {
                    measure.log.LogNotice("Supported constructors:");
                }
                else
                {
                    measure.log.LogNotice("Enum values:");
                }
                measure.log.LogNotice("--------------------------");
                measure.log.LogNotice("RFTypeInfo for [" + type + "]");
                return;
            }
            else if (args[0] == "RFPropertyInfo")
            {
                var prop = measure.Type.GetProperty(args[1]);
                if (prop == null)
                {
                    measure.log.LogError("The property \"" + args[1] + "\" was not found");
                    return;
                }

                measure.log.LogNotice("--------------------------");
                Util.PrintTypeInfo(measure.log, prop.PropertyType);
                if (!prop.PropertyType.IsEnum)
                {
                    measure.log.LogNotice("The type has following supported constructors:");
                }
                else
                {
                    measure.log.LogNotice("The type has following enum values:");
                }
                measure.log.LogNotice("The property is of type " + prop.PropertyType);
                measure.log.LogNotice("--------------------------");
                measure.log.LogNotice("RFPropertyInfo for [" + prop.DeclaringType.Name + "." + prop.Name + "]");
                return;
            }


            var methods = measure.Type.GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(x => x.IsPublic && x.Name == args[0]).ToArray();

            bool successful = false;
            for (int i = 0; i < methods.Length && successful == false; i++)
            {
                Util.TryInvokeMethod(measure.Control, methods[i], args.Skip(1).ToArray(), out successful);
            }

            if (successful == false)
            {
                measure.log.LogError("RainForms.dll: " + args[0] + " is not a valid method in type " + measure.Type);
            }

            measure.log.LogDebug("Received Bang: " + args[0]);
        }
    }
}

