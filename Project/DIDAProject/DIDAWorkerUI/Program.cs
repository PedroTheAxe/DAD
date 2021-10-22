using System;
using System.IO;
using System.Reflection;

namespace DIDAWorkerUI
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(args[0]);
            //TODO: check and maybe refactor
            string className = args[0];
            //string _dllNameTermination = ".dll";
            string _currWorkingDir = Directory.GetCurrentDirectory(); //maybe use Desktop or Downloads
            //DIDAWorker.IDIDAOperator _objLoadedByReflection;

            //Console.WriteLine("current directory: " + _currWorkingDir);
            //change durectory to a more simple one
            /*foreach (string filename in Directory.EnumerateFiles(_currWorkingDir))
            {
                Console.WriteLine("file in cwd: " + filename);
                if (filename.EndsWith(_dllNameTermination))
                {
                    Console.WriteLine(".ddl found");
                    Assembly _dll = Assembly.LoadFrom(filename);
                    Type[] _typeList = _dll.GetTypes();
                    foreach (Type type in _typeList)
                    {
                        Console.WriteLine("type contained in dll: " + type.Name);
                        if (type.Name == className)
                        {
                            Console.WriteLine("Found type to load dynamically: " + className);
                            _objLoadedByReflection = (DIDAWorker.IDIDAOperator)Activator.CreateInstance(type);
                            foreach (MethodInfo method in type.GetMethods())
                            {
                                Console.WriteLine("method from class " + className + ": " + method.Name);
                            }
                            //_objLoadedByReflection.M("success!"); -- seria read, write or update...
                        }
                    }
                }
            }*/

            Console.ReadKey();
        }
    }
}
