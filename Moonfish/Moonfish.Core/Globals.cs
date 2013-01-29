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

        private static tag_type_array tag_types_ = new tag_type_array();

        /// <summary>
        /// A list of each tag_type used in halo 2's retail maps
        /// </summary>
        public static tag_type_array Classes
        {
            get
            {
                return tag_types_;
            }
        }

        private static Dictionary<tag_class, Type> halo_2_classes;

        static Halo2()
        {
            halo_2_classes = new Dictionary<tag_class, Type>(3);
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (!type.IsNested && type.BaseType == typeof(TagBlock))
                {
                    tag_class class_of_tag = (type.GetCustomAttributes(typeof(TagClassAttribute), false)[0] as TagClassAttribute).Tag_Class;
                    halo_2_classes.Add(class_of_tag, type);
                }
            }
        }

        internal static Type GetTypeOf(tag_class class_name)
        {
            return halo_2_classes[class_name];
        }

        internal static TagBlock CreateInstance(tag_class class_name)
        {                
            Type tagblock_type = halo_2_classes[class_name];
            return Activator.CreateInstance(tagblock_type) as TagBlock;
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
