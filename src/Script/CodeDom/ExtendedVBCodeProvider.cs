using System.CodeDom;
using System.Collections;
using Microsoft.VisualBasic;

namespace Sage.SalesLogix.Migration.Script.CodeDom
{
    public sealed class ExtendedVBCodeProvider : ExtendedCodeProvider<VBCodeProvider>
    {
        public ExtendedVBCodeProvider()
        {
            RegisterSubstitution(typeof (CodeDoWhileStatement), SubstituteDoWhile);
            RegisterSubstitution(typeof (CodeExitDoStatement), SubstituteExitDo);
            RegisterSubstitution(typeof (CodeExitForStatement), SubstituteExitFor);
            RegisterSubstitution(typeof (CodeForEachStatement), SubstituteForEach);
            RegisterSubstitution(typeof (CodeOnErrorStatement), SubstituteOnError);
            RegisterSubstitution(typeof (CodeSwitchStatement), SubstituteSwitch);
            RegisterSubstitution(typeof (CodeWithStatement), SubstituteWith);
            RegisterSubstitution(typeof (CodeRectangularArrayCreateExpression), SubstituteRectangularArrayCreate);
            RegisterSubstitution(typeof (CodeUnaryMinusExpression), SubstituteUnaryMinus);
        }

        private CodeObject SubstituteDoWhile(CodeObject obj, CodeObject parent)
        {
            CodeDoWhileStatement doWhileStmt = (CodeDoWhileStatement) obj;

            using (CustomIndentedTextWriter writer = new CustomIndentedTextWriter(Options.IndentString, Indent, true))
            {
                writer.WriteLine("Do");
                WriteStatements(doWhileStmt.Statements, writer, true);
                writer.Write("Loop While ");
                GenerateCodeFromExpression(doWhileStmt.TestExpression, writer, Options);
                return new CodeSnippetStatement(writer.ToString());
            }
        }

        private CodeObject SubstituteExitDo(CodeObject obj, CodeObject parent)
        {
            return new CodeSnippetStatement(GenerateCurrentIndentString() + "Exit Do");
        }

        private CodeObject SubstituteExitFor(CodeObject obj, CodeObject parent)
        {
            return new CodeSnippetStatement(GenerateCurrentIndentString() + "Exit For");
        }

        private CodeObject SubstituteForEach(CodeObject obj, CodeObject parent)
        {
            CodeForEachStatement forEachStmt = (CodeForEachStatement) obj;

            using (CustomIndentedTextWriter writer = new CustomIndentedTextWriter(Options.IndentString, Indent, true))
            {
                writer.Write("For Each ");
                writer.Write(CreateEscapedIdentifier(forEachStmt.ElementName));
                writer.Write(" In ");
                GenerateCodeFromExpression(forEachStmt.EnumerableTarget, writer, Options);
                writer.WriteLine();
                forEachStmt.SuppressFirstStatement();
                WriteStatements(forEachStmt.Statements, writer, true);
                forEachStmt.ExposeFirstStatement();
                writer.Write("Next");
                return new CodeSnippetStatement(writer.ToString());
            }
        }

        private CodeObject SubstituteOnError(CodeObject obj, CodeObject parent)
        {
            return new CodeSnippetStatement(GenerateCurrentIndentString() + (((CodeOnErrorStatement) obj).Enabled
                                                                                 ? "On Error Resume Next"
                                                                                 : "On Error GoTo 0"));
        }

        private CodeObject SubstituteSwitch(CodeObject obj, CodeObject parent)
        {
            CodeSwitchStatement switchStmt = (CodeSwitchStatement) obj;

            using (CustomIndentedTextWriter writer = new CustomIndentedTextWriter(Options.IndentString, Indent, true))
            {
                writer.Write("Select Case ");
                GenerateCodeFromExpression(switchStmt.Target, writer, Options);
                writer.WriteLine();

                using (new IndentedTextWriterIndentBlock(writer))
                {
                    foreach (CodeSwitchOption option in switchStmt.Options)
                    {
                        if (option.Values.Count > 0 && option.Statements.Count > 0)
                        {
                            foreach (CodeExpression optionValue in option.Values)
                            {
                                writer.Write("Case ");
                                GenerateCodeFromExpression(optionValue, writer, Options);
                                writer.WriteLine();
                            }

                            WriteStatements(option.Statements, writer, true);
                        }
                    }

                    if (switchStmt.DefaultStatements.Count > 0)
                    {
                        writer.WriteLine("Case Else");
                        WriteStatements(switchStmt.DefaultStatements, writer, true);
                    }
                }

                writer.Write("End Select");
                return new CodeSnippetStatement(writer.ToString());
            }
        }

        private CodeObject SubstituteWith(CodeObject obj, CodeObject parent)
        {
            CodeWithStatement withStmt = (CodeWithStatement) obj;

            using (CustomIndentedTextWriter writer = new CustomIndentedTextWriter(Options.IndentString, Indent, true))
            {
                writer.Write("With ");

                if (withStmt.ParentWithStatement != null)
                {
                    SetTargetObject(withStmt.Target, withStmt.ParentWithStatement.Target, CodeWithStatement.TargetPlaceholder);
                    GenerateCodeFromExpression(withStmt.Target, writer, Options);
                    SetTargetObject(withStmt.Target, CodeWithStatement.TargetPlaceholder, withStmt.ParentWithStatement.Target);
                }
                else
                {
                    GenerateCodeFromExpression(withStmt.Target, writer, Options);
                }

                writer.WriteLine();
                SetTargetObject(withStmt.Statements, withStmt.Target, CodeWithStatement.TargetPlaceholder);
                WriteStatements(withStmt.Statements, writer, true);
                SetTargetObject(withStmt.Statements, CodeWithStatement.TargetPlaceholder, withStmt.Target);
                writer.Write("End With");
                return new CodeSnippetStatement(writer.ToString());
            }
        }

        private CodeObject SubstituteRectangularArrayCreate(CodeObject obj, CodeObject parent)
        {
            CodeRectangularArrayCreateExpression rectangularArrayCreateExpr = (CodeRectangularArrayCreateExpression) obj;

            using (CustomIndentedTextWriter writer = new CustomIndentedTextWriter(Options.IndentString, Indent, false))
            {
                writer.Write("New ");
                CodeTypeReference typeRef = rectangularArrayCreateExpr.CreateType;

                while (typeRef.ArrayElementType != null)
                {
                    typeRef = typeRef.ArrayElementType;
                }

                writer.Write(GetTypeOutput(typeRef));
                writer.Write("(");
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

                writer.Write(")");
                typeRef = rectangularArrayCreateExpr.CreateType;

                while (typeRef != null && typeRef.ArrayRank > 0)
                {
                    writer.Write("(");

                    for (int i = 1; i < typeRef.ArrayRank; i++)
                    {
                        writer.Write(",");
                    }

                    writer.Write(")");
                    typeRef = typeRef.ArrayElementType;
                }

                writer.Write(" {}");
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

        private CodeExpression _oldTargetExpr;
        private CodeExpression _newTargetExpr;

        private void SetTargetObject(CodeObject obj, CodeExpression oldTargetExpr, CodeExpression newTargetExpr)
        {
            _oldTargetExpr = oldTargetExpr;
            _newTargetExpr = newTargetExpr;
            new CodeDomWalker(obj).Walk(SetTargetObject);
            _oldTargetExpr = null;
            _newTargetExpr = null;
        }

        private void SetTargetObject(IList list, CodeExpression oldTargetExpr, CodeExpression newTargetExpr)
        {
            _oldTargetExpr = oldTargetExpr;
            _newTargetExpr = newTargetExpr;
            new CodeDomWalker(list).Walk(SetTargetObject);
            _oldTargetExpr = null;
            _newTargetExpr = null;
        }

        private void SetTargetObject(ref CodeObject target, CodeObject parent, int indent)
        {
            if (target is CodeMethodReferenceExpression)
            {
                CodeMethodReferenceExpression methodRefExpr = (CodeMethodReferenceExpression) target;

                if (methodRefExpr.TargetObject == _oldTargetExpr)
                {
                    methodRefExpr.TargetObject = _newTargetExpr;
                }
            }
            else if (target is CodePropertyReferenceExpression)
            {
                CodePropertyReferenceExpression propertyExpr = (CodePropertyReferenceExpression) target;

                if (propertyExpr.TargetObject == _oldTargetExpr)
                {
                    propertyExpr.TargetObject = _newTargetExpr;
                }
            }
            else if (target is CodeIndexerExpression)
            {
                CodeIndexerExpression indexerExpr = (CodeIndexerExpression) target;

                if (indexerExpr.TargetObject == _oldTargetExpr)
                {
                    indexerExpr.TargetObject = _newTargetExpr;
                }
            }
        }
    }
}