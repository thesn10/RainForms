using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
using Rainmeter;

namespace PluginRainForms
{
    internal static class Util
    {
        internal static Type TypeFromString(string typename)
        {
            Type type;
            try
            {
                type = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .SelectMany(t => t.GetTypes())
                    .Where(t => string.Equals(t.FullName, typename, StringComparison.OrdinalIgnoreCase))
                    .First();
            }
            catch
            {
                try
                {
                    type = AppDomain.CurrentDomain
                        .GetAssemblies()
                        .SelectMany(t => t.GetTypes())
                        .Where(t => string.Equals(t.Name, typename, StringComparison.OrdinalIgnoreCase))
                        .First();
                }
                catch
                {
                    return null;
                }
            }
            return type;
        }

        internal static void PrintTypeInfo(LogHelper log, Type type)
        {
            if (type.IsEnum)
            {
                foreach (string enumname in Enum.GetNames(type))
                {
                    log.LogNotice(type.Name + "." + enumname);
                }
            }

            int camount = 0;
            var con = type.GetConstructors(BindingFlags.Public);
            if (con != null)
            {

                foreach (ConstructorInfo cinfo in type.GetConstructors())
                {
                    var por = cinfo.GetParameters();
                    bool unsupported = cinfo.GetParameters()
                        .Any(x =>
                        x.ParameterType != typeof(string) &&
                        x.ParameterType != typeof(int) &&
                        x.ParameterType != typeof(bool) &&
                        x.ParameterType != typeof(float) &&
                        x.ParameterType != typeof(double) &&
                        !x.ParameterType.IsEnum);

                    if (unsupported) continue;
                    camount++;

                    string loginfo = type.Name + "(";
                    foreach (ParameterInfo pinfo in cinfo.GetParameters())
                    {
                        loginfo += pinfo.ParameterType + " " + pinfo.Name + ", ";
                    }
                    loginfo = loginfo.Remove(loginfo.Length - 2, 2);
                    loginfo += ")\n";
                    log.LogNotice(loginfo);
                }
            }

            var meth = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
            if (meth != null)
            {
                foreach (MethodInfo minfo in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    if (minfo.ReturnType != type) continue;

                    bool unsupported = minfo.GetParameters()
                        .Any(x =>
                        x.ParameterType != typeof(string) &&
                        x.ParameterType != typeof(int) &&
                        x.ParameterType != typeof(bool) &&
                        x.ParameterType != typeof(float) &&
                        x.ParameterType != typeof(double) &&
                        !x.ParameterType.IsEnum);

                    if (unsupported) continue;
                    camount++;

                    string loginfo = type.Name + "." + minfo.Name + "(";
                    foreach (ParameterInfo pinfo in minfo.GetParameters())
                    {
                        loginfo += pinfo.ParameterType + " " + pinfo.Name + ", ";
                    }
                    loginfo = loginfo.Remove(loginfo.Length - 2, 2);
                    loginfo += ")";
                    log.LogNotice(loginfo);
                }
            }
        }

        internal static object EnumFromString(Type type, string str, bool flag = true)
        {
            if (Enum.GetNames(type).Any(x => x.ToUpper() == str.ToUpper()))
            {
                return Enum.Parse(type, str, true);
            }
            else
            {
                // if the enum is used as a flag
                if (flag)
                {
                    var enumflags = str.Split('|');

                    if (enumflags.Except(Enum.GetNames(type), StringComparer.OrdinalIgnoreCase).Count() > 0)
                    {
                        // found invalid enums
                        return null;
                    }

                    int fEnum = 0;
                    foreach (string s in enumflags)
                    {
                        fEnum = fEnum | (int)Convert.ChangeType(Enum.Parse(type, s), typeof(int));
                    }
                    return Enum.ToObject(type, fEnum);
                }
            }
            return null;
        }

        internal static object ObjectFromString(Rainmeter.API api, Type type, string str)
        {
            string[] strparams = str.Split(',');

            // search and invoke constructors that match the parameters
            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            foreach (ConstructorInfo cinfo in constructors)
            {
                object instance = TryInvokeMethod(null, cinfo, strparams);
                if (instance != null) return instance;
            }

            // no constructor matched, lets search for static methods that return an instance
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(x => x.ReturnType == type);
            foreach (MethodInfo minfo in methods)
            {
                object instance = TryInvokeMethod(null, minfo, strparams);
                if (instance != null) return instance;
            }

            return null;
        }

        internal static object TryInvokeMethod(object instance, MethodBase method, string[] strparams)
        {
            bool b;
            return TryInvokeMethod(instance, method, strparams, out b);
        }

        // Trys to invoke a method with the supplied parameters. Returns null of return type is void or if not possible.
        internal static object TryInvokeMethod(object instance, MethodBase method, string[] strparams, out bool succesfull)
        {
            succesfull = false;
            var parameters = method.GetParameters();
            if (strparams.Length >= parameters.Count(x => !x.IsOptional) && strparams.Length <= parameters.Length)
            {
                try
                {
                    object[] invokeparams = new object[strparams.Length];
                    for (int i = 0; i < strparams.Length; i++)
                    {
                        Type paramtype = parameters[i].ParameterType;
                        //api.Log(Rainmeter.API.LogType.Debug, "paramtype: " + paramtype);
                        if (paramtype == typeof(string))
                        {
                            invokeparams[i] = strparams[i];
                        }
                        else if (paramtype == typeof(int))
                        {
                            int p;
                            if (!int.TryParse(strparams[i], out p)) goto ParamsNotMatching;
                            invokeparams[i] = p;
                        }
                        else if (paramtype == typeof(double))
                        {
                            double p;
                            if (!double.TryParse(strparams[i], out p)) goto ParamsNotMatching;
                            invokeparams[i] = p;
                        }
                        else if (paramtype == typeof(float))
                        {
                            float p;
                            if (!float.TryParse(strparams[i], out p)) goto ParamsNotMatching;
                            invokeparams[i] = p;
                        }
                        else if (paramtype == typeof(bool))
                        {
                            bool p;
                            if (!bool.TryParse(strparams[i], out p)) goto ParamsNotMatching;
                            invokeparams[i] = p;
                        }
                        else
                        {
                            if (paramtype.IsEnum)
                            {
                                var fEnum = Util.EnumFromString(paramtype, strparams[i]);
                                if (fEnum != null)
                                {
                                    invokeparams[i] = fEnum;
                                    continue;
                                }
                            }

                            // non-basic types in constructors are not supported
                            goto ParamsNotMatching;
                        }
                    }

                    if (method is ConstructorInfo cinfo)
                    {
                        object ret = cinfo.Invoke(invokeparams);
                        if (ret != null)
                        {
                            succesfull = true;
                            return ret;
                        }
                    }
                    else if (method is MethodInfo minfo)
                    {
                        object ret = minfo.Invoke(instance, invokeparams);
                        succesfull = true;
                        return ret;
                    }
                }
                catch (Exception e)
                {
                    //api.Log(Rainmeter.API.LogType.Debug, "Ex: " + e.Message);
                }
            }
        ParamsNotMatching:
            return null;
        }

        internal static Color? ColorFromString(string color)
        {
            if (color == "")
            {
                return null;
            }

            string[] rgba = color.Split(',');

            try
            {
                if (rgba.Length == 4)
                {
                    return Color.FromArgb(
                        int.Parse(rgba[3]),
                        int.Parse(rgba[0]),
                        int.Parse(rgba[1]),
                        int.Parse(rgba[2]));
                }
                else if (rgba.Length == 3)
                {
                    return Color.FromArgb(
                        int.Parse(rgba[0]),
                        int.Parse(rgba[1]),
                        int.Parse(rgba[2]));
                }
            }
            catch { }
            return null;
        }
    }

    internal class LogHelper
    {
        private Rainmeter.API api;
        internal LogHelper(API api)
        {
            this.api = api;
        }

        internal void LogDebug(string message)
        {
#if DEBUG
            api.Log(API.LogType.Debug, message);
#endif
        }

        internal void LogError(string message)
        {
            api.Log(API.LogType.Error, message);
        }

        internal void LogNotice(string message)
        {
            api.Log(API.LogType.Notice, message);
        }

        internal void LogWarning(string message)
        {
            api.Log(API.LogType.Warning, message);
        }

        internal void LogParameterNotValid(string param, string paramname)
        {
            LogError(
                "RainForms.dll: \"" +
                param +
                "\" is not a valid " +
                paramname);
        }

        internal void LogPropertyValueNotValid(string value, PropertyInfo prop)
        {
            LogError(
                "RainForms.dll: The value " +
                value +
                " for property " +
                prop.Name +
                " is not valid, it needs to be of type " +
                prop.PropertyType);
        }
    }
}
