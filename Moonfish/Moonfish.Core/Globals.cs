using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Moonfish.Core
{
    /// <summary>
    /// This static class holds all globals for Halo 2 and useful methods
    /// </summary>
    public static class Halo2
    {
        public const int nullptr = 0;

        /// <summary>
        /// A list of each tag_type used in halo 2's retail maps
        /// </summary>
        public static tag_type_array Classes
        {
            get { return tag_types_; }
        }
        /// <summary>
        /// A list of all standard strings in Halo 2
        /// </summary>
        public static GlobalStrings Strings { get { return strings_; } }

        private static tag_type_array tag_types_ = new tag_type_array();
        private static GlobalStrings strings_ = new GlobalStrings();
        private static Dictionary<TagClass, Type> halo_2_classes;

        static Halo2()
        {
            halo_2_classes = new Dictionary<TagClass, Type>(3);
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (!type.IsNested && type.BaseType == typeof(TagBlock))
                {
                    if(type.GetCustomAttributes(typeof(TagClassAttribute), false).Length > 0){
                    TagClass class_of_tag = (type.GetCustomAttributes(typeof(TagClassAttribute), false)[0] as TagClassAttribute).Tag_Class;
                    halo_2_classes.Add(class_of_tag, type);
                }}
            }
        }

        internal static Type GetTypeOf(TagClass class_name)
        {
            return halo_2_classes[class_name];
        }

        public static TagBlock CreateInstance(TagClass class_name)
        {                
            Type tagblock_type = halo_2_classes[class_name];
            return Activator.CreateInstance(tagblock_type) as TagBlock;
        }
    }

    public static class Log
    {
        public delegate void LogMessageHandler(string message);
        public static LogMessageHandler OnLog;

        internal static void Error(string message)
        {
            LogMessage("Error", message);
        }

        internal static void Warn(string message)
        {
            LogMessage("Warning", message);
        }

        static void LogMessage(string token, string message)
        {
            if (OnLog != null)
                OnLog(string.Format("{0}: {1}", token,  message));
        }

        internal static void Info(string message)
        {
            LogMessage("Info", message);
        }
    }

    public static class StaticBenchmark
    {
        static Stopwatch Timer = new Stopwatch();
        static string result;

        public static void Begin()
        {
            Timer.Start();
        }
        public static void End()
        {
            Timer.Stop();
            result = Timer.ElapsedMilliseconds.ToString() + " Milliseconds";
            Timer.Reset();
        }
        public static string Result { get { return result; } }

        public static new string ToString()
        {
            return Result;
        }
    }
}