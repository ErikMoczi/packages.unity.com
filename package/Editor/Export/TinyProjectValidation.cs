

using UnityEngine;

namespace Unity.Tiny
{
    internal class TinyProjectValidation
    {
        public static bool Validate(TinyProject project)
        {
            var visitor = new ProjectValidationVisitor();
            project.Visit(visitor);
            return visitor.Valid;
        }

        private class ProjectValidationVisitor : TinyProject.Visitor
        {
            public bool Valid { get; private set; } = true;
            
            public override void VisitType(TinyType type)
            {
                if (TinyScriptUtility.IsReservedKeyword(type.Name))
                {
                    Valid = false;
                    Debug.LogError($"[{TinyConstants.ApplicationName}] TypeName=[{type.Name}] is a reserved keyword");
                }

                foreach (var field in type.Fields)
                {
                    var fieldType = field.FieldType.Dereference(type.Registry);

                    if (null == fieldType)
                    {
                        Valid = false;
                        Debug.LogError($"[{TinyConstants.ApplicationName}] MissingFieldType FieldType=[{field.FieldType.Name}] Field=[{type.Name}.{field.Name}]");
                    }
                }
            }
        }
    }
}


