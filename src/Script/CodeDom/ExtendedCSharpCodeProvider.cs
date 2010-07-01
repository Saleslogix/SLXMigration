using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.IO;
using System.Reflection;
using Microsoft.CSharp;

namespace Sage.SalesLogix.Migration.Script.CodeDom
{
    public sealed class ExtendedCSharpCodeProvider : ExtendedCodeProvider<CSharpCodeProvider>
    {
        public ExtendedCSharpCodeProvider()
        {
            RegisterSubstitution(typeof (CodeDestructor), SubstituteDestructor);
            RegisterSubstitution(typeof (CodeDoWhileStatement), SubstituteDoWhile);
            RegisterSubstitution(typeof (CodeExitDoStatement), SubstituteExitDo);
            RegisterSubstitution(typeof (CodeExitForStatement), SubstituteExitFor);
            RegisterSubstitution(typeof (CodeForEachStatement), SubstituteForEach);
            RegisterSubstitution(typeof (CodeSwitchStatement), SubstituteSwitch);
            RegisterSubstitution(typeof (CodeWhileStatement), SubstituteWhile);
            RegisterSubstitution(typeof (CodeWithStatement), SubstituteWith);
            RegisterSubstitution(typeof (CodeRectangularArrayCreateExpression), SubstituteRectangularArrayCreate);
            RegisterSubstitution(typeof (CodeUnaryMinusExpression), SubstituteUnaryMinus);
        }

        private CodeObject SubstituteDestructor(CodeObject obj, CodeObject parent)
        {
            CodeDestructor destructor = (CodeDestructor) obj;
            CodeTypeDeclaration typeDecl = (CodeTypeDeclaration) parent;

            using (CustomIndentedTextWriter writer = new CustomIndentedTextWriter(Options.IndentString, Indent, true))
            {
                if (destructor.CustomAttributes.Count > 0)
                {
                    GenerateAttributes(destructor.CustomAttributes, writer);
                }

                writer.Write("~");

                if (typeDecl != null)
                {
                    writer.Write(CreateEscapedIdentifier(typeDecl.Name));
                }

                writer.Write("()");
                OutputStartingBrace(writer);
                WriteStatements(destructor.Statements, writer, true);
                writer.WriteLine("}");
                return new CodeSnippetTypeMember(writer.ToString());
            }
        }

        private CodeObject SubstituteDoWhile(CodeObject obj, CodeObject parent)
        {
            CodeDoWhileStatement doWhileStmt = (CodeDoWhileStatement) obj;

            using (CustomIndentedTextWriter writer = new CustomIndentedTextWriter(Options.IndentString, Indent, true))
            {
                writer.Write("do");
                OutputStartingBrace(writer);
                WriteStatements(doWhileStmt.Statements, writer, true);
                writer.Write("} while ");
                WriteExpressionWithBrackets(doWhileStmt.TestExpression, writer);
                writer.Write(";");
                return new CodeSnippetStatement(writer.ToString());
            }
        }

        private CodeObject SubstituteExitDo(CodeObject obj, CodeObject parent)
        {
            return new CodeSnippetStatement(GenerateCurrentIndentString() + "break;");
        }

        private CodeObject SubstituteExitFor(CodeObject obj, CodeObject parent)
        {
            return new CodeSnippetStatement(GenerateCurrentIndentString() + "break;");
        }

        private CodeObject SubstituteForEach(CodeObject obj, CodeObject parent)
        {
            CodeForEachStatement forEachStmt = (CodeForEachStatement) obj;

            using (CustomIndentedTextWriter writer = new CustomIndentedTextWriter(Options.IndentString, Indent, true))
            {
                string specialElementName = "_" + forEachStmt.ElementName;
                writer.Write("foreach (");
                writer.Write(GetTypeOutput(forEachStmt.ElementType));
                writer.Write(" ");
                writer.Write(CreateEscapedIdentifier(specialElementName));
                writer.Write(" in ");
                GenerateCodeFromExpression(forEachStmt.EnumerableTarget, writer, Options);
                writer.Write(")");
                OutputStartingBrace(writer);
                forEachStmt.SuppressFirstStatement();
                RenameVariablesReferences(forEachStmt.Statements, forEachStmt.ElementName, specialElementName);
                WriteStatements(forEachStmt.Statements, writer, true);
                RenameVariablesReferences(forEachStmt.Statements, specialElementName, forEachStmt.ElementName);
                forEachStmt.ExposeFirstStatement();
                writer.Write("}");
                return new CodeSnippetStatement(writer.ToString());
            }
        }

        private CodeObject SubstituteSwitch(CodeObject obj, CodeObject parent)
        {
            CodeSwitchStatement switchStmt = (CodeSwitchStatement) obj;

            using (CustomIndentedTextWriter writer = new CustomIndentedTextWriter(Options.IndentString, Indent, true))
            {
                writer.Write("switch ");
                WriteExpressionWithBrackets(switchStmt.Target, writer);
                OutputStartingBrace(writer);

                using (new IndentedTextWriterIndentBlock(writer))
                {
                    foreach (CodeSwitchOption option in switchStmt.Options)
                    {
                        if (option.Values.Count > 0 && option.Statements.Count > 0)
                        {
                            bool first = true;

                            foreach (CodeExpression optionValue in option.Values)
                            {
                                if (first)
                                {
                                    first = false;
                                }
                                else
                                {
                                    writer.WriteLine();
                                }

                                writer.Write("case ");
                                GenerateCodeFromExpression(optionValue, writer, Options);
                                writer.Write(":");
                            }

                            OutputCaseStatements(option.Statements, writer);
                        }
                    }

                    if (switchStmt.DefaultStatements.Count > 0)
                    {
                        writer.Write("default:");
                        OutputCaseStatements(switchStmt.DefaultStatements, writer);
                    }
                }

                writer.Write("}");
                return new CodeSnippetStatement(writer.ToString());
            }
        }

        private CodeObject SubstituteWhile(CodeObject obj, CodeObject parent)
        {
            CodeWhileStatement whileStmt = (CodeWhileStatement) obj;

            using (CustomIndentedTextWriter writer = new CustomIndentedTextWriter(Options.IndentString, Indent, true))
            {
                writer.Write("while ");
                WriteExpressionWithBrackets(whileStmt.TestExpression, writer);
                OutputStartingBrace(writer);
                WriteStatements(whileStmt.Statements, writer, true);
                writer.Write("}");
                return new CodeSnippetStatement(writer.ToString());
            }
        }

        private CodeObject SubstituteWith(CodeObject obj, CodeObject parent)
        {
            using (CustomIndentedTextWriter writer = new CustomIndentedTextWriter(Options.IndentString, Indent, true))
            {
                writer.WriteLine("{");
                WriteStatements(((CodeWithStatement) obj).Statements, writer, true);
                writer.Write("}");
                return new CodeSnippetStatement(writer.ToString());
            }
        }

        private CodeObject SubstituteRectangularArrayCreate(CodeObject obj, CodeObject parent)
        {
            CodeRectangularArrayCreateExpression rectangularArrayCreateExpr = (CodeRectangularArrayCreateExpression) obj;

            using (CustomIndentedTextWriter writer = new CustomIndentedTextWriter(Options.IndentString, Indent, false))
            {
                writer.Write("new ");
                CodeTypeReference typeRef = rectangularArrayCreateExpr.CreateType;

                while (typeRef.ArrayElementType != null)
                {
                    typeRef = typeRef.ArrayElementType;
                }

                writer.Write(GetTypeOutput(typeRef));
                writer.Write("[");
                bool isFirst = true;

                foreach (CodeExpression expr in rectangularArrayCreateExpr.Lengths)
                {
                    if (isFirst)
                    {
                        isFirst = false;
                    }
                    else
                    {
                        writer.Write(", ");
                    }

                    GenerateCodeFromExpression(expr, writer, Options);
                }

                writer.Write("]");
                typeRef = rectangularArrayCreateExpr.CreateType;

                while (typeRef != null && typeRef.ArrayRank > 0)
                {
                    writer.Write("[");

                    for (int i = 1; i < typeRef.ArrayRank; i++)
                    {
                        writer.Write(",");
                    }

                    writer.Write("]");
                    typeRef = typeRef.ArrayElementType;
                }

                return new CodeSnippetExpression(writer.ToString());
            }
        }

        private static CodeObject SubstituteUnaryMinus(CodeObject obj, CodeObject parent)
        {
            return new CodeMethodInvokeExpression(
                null,
                "-",
                ((CodeUnaryMinusExpression) obj).Target);
        }

        //---------------------------------------

        private void WriteExpressionWithBrackets(CodeExpression expr, TextWriter writer)
        {
            bool requiresBrackets = !(expr is CodeBinaryOperatorExpression);

            if (requiresBrackets)
            {
                writer.Write("(");
            }

            GenerateCodeFromExpression(expr, writer, Options);

            if (requiresBrackets)
            {
                writer.Write(")");
            }
        }

        private void OutputCaseStatements(CodeStatementCollection statements, IndentedTextWriter writer)
        {
            OutputStartingBrace(writer);

            using (new IndentedTextWriterIndentBlock(writer))
            {
                WriteStatements(statements, writer, false);
                writer.WriteLine("break;");
            }

            writer.WriteLine("}");
        }

        //---------------------------------------

        private Type _generatorType;
        private object _generator;
        private FieldInfo _outputField;
        private FieldInfo _optionsField;

        private delegate void GenerateAttributesHandler(CodeAttributeDeclarationCollection attributes);

        private delegate void OutputStartingBraceHandler();

        private GenerateAttributesHandler _generateAttributesMethod;
        private OutputStartingBraceHandler _outputStartingBraceMethod;

        private void GenerateAttributes(CodeAttributeDeclarationCollection attributes, IndentedTextWriter writer)
        {
            InvokeMethod("GenerateAttributes", new Type[] {typeof (CodeAttributeDeclarationCollection)}, ref _generateAttributesMethod, writer)(attributes);
        }

        private void OutputStartingBrace(IndentedTextWriter writer)
        {
            InvokeMethod("OutputStartingBrace", new Type[] {}, ref _outputStartingBraceMethod, writer)();
        }

        private T InvokeMethod<T>(string methodName, Type[] types, ref T field, IndentedTextWriter writer)
            where T : class
        {
            if (_generator == null)
            {
#pragma warning disable 618,612
                _generatorType = CreateGenerator().GetType();
#pragma warning restore 618,612
                _generator = Activator.CreateInstance(_generatorType, true);
                _outputField = _generatorType.GetField("output", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                _optionsField = _generatorType.GetField("options", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            }

            if (field == null)
            {
                field = (T) (object) Delegate.CreateDelegate(
                                         typeof (T),
                                         _generator,
                                         _generatorType.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, types, null));
            }

            _outputField.SetValue(_generator, writer);
            _optionsField.SetValue(_generator, Options);
            return field;
        }

        //---------------------------------------

        private string _beforeVariableName;
        private string _afterVariableName;

        public void RenameVariablesReferences(IList stmts, string beforeVariableName, string afterVariableName)
        {
            _beforeVariableName = beforeVariableName;
            _afterVariableName = afterVariableName;
            new CodeDomWalker(stmts).Walk(RenameVariablesReferences);
            _beforeVariableName = null;
            _afterVariableName = null;
        }

        private void RenameVariablesReferences(ref CodeObject target, CodeObject parent, int indent)
        {
            CodeVariableReferenceExpression variableRefExpr = target as CodeVariableReferenceExpression;

            if (variableRefExpr != null && variableRefExpr.VariableName == _beforeVariableName)
            {
                variableRefExpr.VariableName = _afterVariableName;
            }
        }
    }
}