#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace Utils
{
    [InitializeOnLoad]
    public static class CodeAnalyzer
    {
        private static string outputDirectory = "CodeAnalysis";
        private static string[] namespaceFilters = new string[] { "BattleField", "Merge2" };

        // Static constructor that is called when Unity is started or scripts are recompiled
        static CodeAnalyzer()
        {
            CompilationPipeline.compilationFinished += OnCompilationFinished;
        }

        // Will be called when scripts are recompiled
        private static void OnCompilationFinished(object obj)
        {
            Debug.Log("Compilation finished - running code analysis...");
            AnalyzeCode();
        }

        // Add menu item to manually trigger code analysis
        [MenuItem("Expecto/Code/Analyze Code", priority = 1000)]
        private static void AnalyzeCodeMenuItem()
        {
            AnalyzeCode();
        }

        private static void AnalyzeCode()
        {
            Debug.Log("Starting code analysis...");

            List<ClassInfo> classes = new List<ClassInfo>();

            // Get all loaded assemblies
            System.Reflection.Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (System.Reflection.Assembly assembly in assemblies)
            {
                try
                {
                    // Get all types in the assembly
                    Type[] types = assembly.GetTypes();

                    foreach (Type type in types)
                    {
                        // Skip compiler-generated classes
                        if (type.Name.Contains("<") || type.Name.StartsWith("__") ||
                            type.Name.Contains("DisplayClass") || type.Name.Contains("AnonymousType") ||
                            type.Name.Contains("$"))
                        {
                            continue; // Skip this type
                        }

                        // Skip types that inherit from MulticastDelegate (delegates)
                        if (type.BaseType?.Name == "MulticastDelegate")
                        {
                            continue; // Skip delegates
                        }

                        // Skip nested/inner classes
                        if (type.IsNested)
                        {
                            continue; // Skip nested classes
                        }

                        // Filter by namespace if needed
                        if (type.Namespace != null && namespaceFilters.Any(filter => type.Namespace.StartsWith(filter)))
                        {
                            ClassInfo classInfo = new ClassInfo
                            {
                                Name = type.Name,
                                Namespace = type.Namespace,
                                BaseClass = type.BaseType?.FullName,
                                Fields = new List<FieldData>(),
                                Methods = new List<MethodData>()
                            };

                            // Get properties first and remember their names to avoid showing similar fields
                            HashSet<string> propertyNames = new HashSet<string>();
                            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
                            foreach (PropertyInfo property in properties)
                            {
                                // Only include properties that are declared in this type (not inherited)
                                if (property.DeclaringType != type)
                                {
                                    continue;
                                }

                                string getterModifier = "-";
                                string setterModifier = "-";

                                // Determine access modifier for getter
                                if (property.GetMethod != null)
                                {
                                    getterModifier = GetAccessModifierSymbol(property.GetMethod);
                                }

                                // Determine access modifier for setter
                                if (property.SetMethod != null)
                                {
                                    setterModifier = GetAccessModifierSymbol(property.SetMethod);
                                }

                                string typeName = GetFormattedTypeName(property.PropertyType);

                                classInfo.Fields.Add(new FieldData
                                {
                                    Name = property.Name,
                                    Type = typeName,
                                    GetterModifier = getterModifier,
                                    SetterModifier = setterModifier,
                                    IsProperty = true
                                });

                                // Store both exact name and lowercase version for case-insensitive comparison
                                propertyNames.Add(property.Name);
                                propertyNames.Add(property.Name.ToLowerInvariant());
                            }

                            // Get fields
                            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                            foreach (FieldInfo field in fields)
                            {
                                // Skip fields declared in parent classes
                                if (field.DeclaringType != type)
                                {
                                    continue;
                                }

                                // Skip backing fields for auto-properties
                                if (field.Name.Contains("k__BackingField"))
                                {
                                    continue;
                                }

                                string fieldName = field.Name;

                                // Skip fields that match property names or case-insensitive versions
                                // Check if there's a property with the same name or a capitalized version of this field
                                string capitalizedFieldName = char.ToUpperInvariant(fieldName[0]) + fieldName.Substring(1);
                                if (propertyNames.Contains(fieldName) || propertyNames.Contains(capitalizedFieldName))
                                {
                                    continue;
                                }

                                string accessModifier = GetAccessModifierSymbol(field);
                                string typeName = GetFormattedTypeName(field.FieldType);

                                classInfo.Fields.Add(new FieldData
                                {
                                    Name = fieldName,
                                    Type = typeName,
                                    AccessModifier = accessModifier
                                });
                            }

                            // Get methods
                            MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
                            foreach (MethodInfo method in methods)
                            {
                                string methodName = method.Name;

                                // Skip property accessors
                                if (methodName.StartsWith("get_") || methodName.StartsWith("set_"))
                                {
                                    continue; // Skip property accessor methods
                                }

                                // Skip event accessors
                                if (methodName.StartsWith("add_") || methodName.StartsWith("remove_"))
                                {
                                    continue; // Skip event accessor methods
                                }

                                // Skip anonymous methods and compiler-generated methods
                                if (methodName.Contains("<") && (methodName.Contains(">b__") || methodName.Contains(">c__")))
                                {
                                    continue; // Skip this method
                                }

                                string accessModifier = GetAccessModifierSymbol(method);
                                var parameters = method.GetParameters();
                                var paramList = parameters != null && parameters.Length > 0
                                    ? parameters.Select(p => new ParameterData
                                    {
                                        Type = GetFormattedTypeName(p.ParameterType),
                                        Name = string.IsNullOrEmpty(p.Name) ? "param" : p.Name
                                    }).ToList()
                                    : new List<ParameterData>();

                                // Format other compiler-generated methods to be more readable
                                if (methodName.Contains("__") || (methodName.Contains("<") && methodName.Contains(">")))
                                {
                                    if (methodName.StartsWith("<"))
                                    {
                                        int startIndex = methodName.IndexOf('<') + 1;
                                        int endIndex = methodName.IndexOf('>');
                                        if (startIndex > 0 && endIndex > startIndex)
                                        {
                                            string parsedName = methodName.Substring(startIndex, endIndex - startIndex);
                                            methodName = $"Generated method for {parsedName}";
                                        }
                                    }
                                }

                                classInfo.Methods.Add(new MethodData
                                {
                                    Name = methodName,
                                    AccessModifier = accessModifier,
                                    ReturnType = GetFormattedTypeName(method.ReturnType),
                                    Parameters = paramList
                                });
                            }

                            classes.Add(classInfo);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error analyzing assembly: {e.Message}");
                }
            }

            // Export to XML, split by namespace
            ExportToXmlByNamespace(classes);

            Debug.Log($"Code analysis completed. Results saved to {outputDirectory} directory");
        }

        private static string GetAccessModifierSymbol(MethodInfo method)
        {
            if (method.IsPublic)
                return "+";
            if (method.IsPrivate)
                return "-";
            if (method.IsFamily) // protected
                return "-";
            if (method.IsFamilyOrAssembly) // protected internal
                return "-";
            if (method.IsAssembly) // internal
                return "~"; // using ~ for internal

            return "-"; // default to private
        }

        private static string GetAccessModifierSymbol(FieldInfo field)
        {
            if (field.IsPublic)
                return "+";
            if (field.IsPrivate)
                return "-";
            if (field.IsFamily) // protected
                return "-";
            if (field.IsFamilyOrAssembly) // protected internal
                return "-";
            if (field.IsAssembly) // internal
                return "~"; // using ~ for internal

            return "-"; // default to private
        }

        private static string GetFormattedTypeName(Type type)
        {
            // Handle array types
            if (type.IsArray)
            {
                Type elementType = type.GetElementType();
                string elementTypeName = GetFormattedTypeName(elementType);
                return $"{elementTypeName}[]";
            }

            // Convert system types to their C# aliases
            string typeName = GetCSharpTypeName(type.Name);

            if (!type.IsGenericType)
                return typeName;

            // Get the generic type name without the `n suffix
            string baseName = type.Name;
            int backtickIndex = baseName.IndexOf('`');
            if (backtickIndex > 0)
            {
                baseName = baseName.Substring(0, backtickIndex);
            }

            // Convert base name
            baseName = GetCSharpTypeName(baseName);

            // Get the generic arguments
            Type[] genericArgs = type.GetGenericArguments();
            string[] argNames = genericArgs.Select(GetFormattedTypeName).ToArray();

            // Format using parentheses instead of angle brackets to avoid XML encoding issues
            return $"{baseName}<{string.Join(", ", argNames)}>";
        }

        private static string GetCSharpTypeName(string typeName)
        {
            // Map .NET type names to C# aliases
            switch (typeName)
            {
                case "Int32": return "int";
                case "Int64": return "long";
                case "Single": return "float";
                case "Double": return "double";
                case "Boolean": return "bool";
                case "Char": return "char";
                case "Byte": return "byte";
                case "Int16": return "short";
                case "UInt16": return "ushort";
                case "UInt32": return "uint";
                case "UInt64": return "ulong";
                case "SByte": return "sbyte";
                case "Decimal": return "decimal";
                case "String": return "string";
                case "Object": return "object";
                case "Void": return "void";
                default: return typeName;
            }
        }

        private static void ExportToXmlByNamespace(List<ClassInfo> classes)
        {
            // Group classes by namespace
            var namespaceGroups = classes.GroupBy(c => c.Namespace).ToList();

            // Create output directory if it doesn't exist
            string outputDirPath = Path.Combine(Application.dataPath + "/..", outputDirectory);
            if (!Directory.Exists(outputDirPath))
            {
                Directory.CreateDirectory(outputDirPath);
            }

            // Create and save a file for each namespace
            foreach (var namespaceGroup in namespaceGroups)
            {
                string namespaceName = namespaceGroup.Key;

                // Create safe filename from namespace
                string safeFilename = namespaceName.Replace(".", "_") + ".xml";
                string filePath = Path.Combine(outputDirPath, safeFilename);

                // Create XML document
                XmlDocument doc = new XmlDocument();

                // Create root element
                XmlElement root = doc.CreateElement("CodeAnalysis");
                root.SetAttribute("Namespace", namespaceName);
                doc.AppendChild(root);

                // Add comment about XML encoding
                XmlComment comment = doc.CreateComment(" Note: Generic types use (Type1, Type2) format instead of angle brackets due to XML encoding. ");
                root.AppendChild(comment);

                // Sort classes by name
                var sortedClasses = namespaceGroup.OrderBy(c => c.Name).ToList();

                // Add classes
                foreach (ClassInfo classInfo in sortedClasses)
                {
                    XmlElement classElement = doc.CreateElement("Class");

                    // Add class attributes
                    XmlElement nameElement = doc.CreateElement("Name");
                    nameElement.InnerText = classInfo.Name;
                    classElement.AppendChild(nameElement);

                    if (!string.IsNullOrEmpty(classInfo.BaseClass))
                    {
                        // Strip the namespace from the base class name
                        string baseClassName = classInfo.BaseClass;
                        int lastDotIndex = baseClassName.LastIndexOf('.');
                        if (lastDotIndex >= 0 && lastDotIndex < baseClassName.Length - 1)
                        {
                            baseClassName = baseClassName.Substring(lastDotIndex + 1);
                        }

                        // Skip adding Object and ValueType as a base class
                        if (baseClassName != "Object" && baseClassName != "ValueType")
                        {
                            XmlElement baseClassElement = doc.CreateElement("BaseClass");
                            baseClassElement.InnerText = baseClassName;
                            classElement.AppendChild(baseClassElement);
                        }
                    }

                    // Sort fields - public fields first
                    var sortedFields = classInfo.Fields
                        .OrderBy(f =>
                        {
                            // Order by visibility:
                            // 1. Public properties
                            // 2. Public fields
                            // 3. Public properties with private/protected setter
                            // 4. All others

                            if (f.IsProperty)
                            {
                                if (f.GetterModifier == "+")
                                {
                                    // Public properties with public getter
                                    if (f.SetterModifier == "+")
                                    {
                                        // Public property with public setter
                                        return 0;
                                    }
                                    else
                                    {
                                        // Public properties with private/protected setter
                                        return 2;
                                    }
                                }
                                else
                                {
                                    // Non-public properties
                                    return 3;
                                }
                            }
                            else
                            {
                                // Regular fields
                                if (f.AccessModifier == "+")
                                {
                                    // Public fields
                                    return 1;
                                }
                                else
                                {
                                    // Non-public fields
                                    return 4;
                                }
                            }
                        })
                        .ThenBy(f => f.Name); // Then sort by name

                    // Add fields
                    XmlElement fieldsElement = doc.CreateElement("Fields");
                    classElement.AppendChild(fieldsElement);

                    foreach (FieldData field in sortedFields)
                    {
                        XmlElement fieldElement = doc.CreateElement("Field");
                        string fieldText;

                        if (field.IsProperty)
                        {
                            // Use combined modifier for properties (e.g. "+-" for public getter, private setter)
                            string combinedModifier = field.GetterModifier + field.SetterModifier;
                            fieldText = $"{combinedModifier} {field.Name}: {field.Type}";
                        }
                        else
                        {
                            fieldText = $"{field.AccessModifier} {field.Name}: {field.Type}";
                        }

                        fieldElement.InnerText = fieldText;
                        fieldsElement.AppendChild(fieldElement);
                    }

                    // Sort methods - public methods first
                    var sortedMethods = classInfo.Methods
                        .OrderBy(m => m.AccessModifier != "+") // Public methods first
                        .ThenBy(m => m.Name); // Then by name

                    // Add methods
                    XmlElement methodsElement = doc.CreateElement("Methods");
                    classElement.AppendChild(methodsElement);

                    foreach (MethodData method in sortedMethods)
                    {
                        XmlElement methodElement = doc.CreateElement("Method");

                        // Format parameters as "Type name, Type name, ..."
                        string parameters = method.Parameters != null && method.Parameters.Any()
                            ? string.Join(", ", method.Parameters.Select(p => $"{p.Type} {p.Name}"))
                            : "";

                        methodElement.InnerText = $"{method.AccessModifier} {method.Name}({parameters}): {method.ReturnType}";
                        methodsElement.AppendChild(methodElement);
                    }

                    root.AppendChild(classElement);
                }

                // Save to file
                doc.Save(filePath);
                Debug.Log($"Created file for namespace {namespaceName}: {filePath}");
            }
        }

        private class ClassInfo
        {
            public string Name { get; set; }
            public string Namespace { get; set; }
            public string BaseClass { get; set; }
            public List<FieldData> Fields { get; set; }
            public List<MethodData> Methods { get; set; }
        }

        private class FieldData
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public string AccessModifier { get; set; }
            public string GetterModifier { get; set; }
            public string SetterModifier { get; set; }
            public bool IsProperty { get; set; }
        }

        private class MethodData
        {
            public string Name { get; set; }
            public string AccessModifier { get; set; }
            public string ReturnType { get; set; }
            public List<ParameterData> Parameters { get; set; }
        }

        private class ParameterData
        {
            public string Type { get; set; }
            public string Name { get; set; }
        }
    }
}
#endif