using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using GoldParser;
using Microsoft.VisualBasic.CompilerServices;
using Sage.SalesLogix.Migration.Script.CodeDom;

namespace Sage.SalesLogix.Migration.Script.VBSParser
{
    public sealed class Parser
    {
        private static readonly CodeTypeReference _objectTypeReference;
        private static readonly object _newLinePlaceholder;

        private readonly Grammar _grammar;
        private GoldParser.Parser _parser;

        private readonly SortedList<int, CodeCommentStatement> _comments;
        private CodeAttributeDeclaration _defaultAttribute;
        private int? _elseLine;

        static Parser()
        {
            _objectTypeReference = new CodeTypeReference(typeof (object));
            _newLinePlaceholder = new object();
        }

        public Parser()
        {
            _comments = new SortedList<int, CodeCommentStatement>();

            using (Stream stream = GetType().Assembly.GetManifestResourceStream(GetType(), "VBScript.cgt"))
            {
                _grammar = new Grammar(new BinaryReader(stream));
            }
        }

        public CodeTypeDeclaration Parse(string code)
        {
            using (TextReader reader = new StringReader(code))
            {
                return Parse(reader);
            }
        }

        public CodeTypeDeclaration Parse(TextReader reader)
        {
            try
            {
                _parser = new GoldParser.Parser(reader, _grammar);
                _parser.TrimReductions = true;

                Debug.Assert(_comments.Count == 0);
                Debug.Assert(_defaultAttribute == null);
                Debug.Assert(_elseLine == null);

                while (true)
                {
                    ParseMessage message = _parser.Parse();

                    switch (message)
                    {
                        case ParseMessage.Accept:
                            return ParseAccept();
                        case ParseMessage.Reduction:
                            {
                                object value = ParseReduction();
                                int? line = null;

                                if (value is CodeTypeMember || value is CodeStatement)
                                {
                                    CodeObject obj = (CodeObject) value;
                                    line = GetLine(obj);

                                    if (line == null)
                                    {
                                        for (int i = 0; i < _parser.ReductionCount; i++)
                                        {
                                            if (GetTokenValue(i) == _newLinePlaceholder)
                                            {
                                                line = GetTokenLine(i);
                                                SetLine(obj, line);
                                                break;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    for (int i = 0; i < _parser.ReductionCount && line == null; i++)
                                    {
                                        line = GetTokenLine(i);
                                    }
                                }

                                _parser.TokenSyntaxNode = new Token(value, line);
                            }
                            break;
                        case ParseMessage.TokenRead:
                            _parser.TokenSyntaxNode = new Token(_parser.TokenText, _parser.LineNumber);
                            break;
                        case ParseMessage.CommentLineRead:
                            {
                                int line = _parser.LineNumber;
                                CodeCommentStatement commentStatement = new CodeCommentStatement(_parser.CommentText.Substring(1));
                                _comments.Add(line, commentStatement);
                            }
                            break;
                        case ParseMessage.SyntaxError:
                        case ParseMessage.LexicalError:
                        case ParseMessage.InternalError:
                        case ParseMessage.NotLoadedError:
                        case ParseMessage.CommentError:
                            throw new ParserException(string.Format("{0} on line {1}", message, _parser.LineNumber));
                        case ParseMessage.CommentBlockRead:
                        case ParseMessage.Empty:
                        default:
                            throw new ParserException(string.Format("Unexpected message '{0}' on line {1}", message, _parser.LineNumber));
                    }
                }
            }
            catch (Exception)
            {
                _comments.Clear();
                throw;
            }
            finally
            {
                Debug.Assert(_comments.Count == 0);
                Debug.Assert(_defaultAttribute == null);

                _lines = null;
                _elseLines = null;
                _lastLines = null;
            }
        }

        private CodeTypeDeclaration ParseAccept()
        {
            CodeTypeDeclaration typeDecl = new CodeTypeDeclaration();
            typeDecl.Attributes = 0;
            object value = ((Token) _parser.TokenSyntaxNode).Value;

            if (value != null)
            {
                CodeConstructor constructor = new CodeConstructor();

                if (value is CodeObject)
                {
                    value = new CodeObject[] {(CodeObject) value};
                }

                foreach (CodeObject obj in (IEnumerable) value)
                {
                    if (obj is CodeVariableDeclarationStatement)
                    {
                        CodeVariableDeclarationStatement variableDeclStmt = (CodeVariableDeclarationStatement) obj;
                        CodeMemberField memberField = new CodeMemberField(
                            variableDeclStmt.Type,
                            variableDeclStmt.Name);
                        memberField.InitExpression = variableDeclStmt.InitExpression;
                        memberField.Attributes = 0;
                        SetLine(memberField, GetLine(variableDeclStmt));
                        IncorporateComments(memberField);
                        typeDecl.Members.Add(memberField);
                    }
                    else if (obj is CodeStatement)
                    {
                        CodeStatement statement = (CodeStatement) obj;
                        int? firstLine = GetLine(statement);
                        int extraCount = _comments.Count;

                        for (int j = 0; j < extraCount && _comments.Keys[0] < firstLine; j++)
                        {
                            constructor.Statements.Add(_comments.Values[0]);
                            _comments.RemoveAt(0);
                        }

                        IncorporateComments(statement);
                        constructor.Statements.Add(statement);
                    }
                    else
                    {
                        CodeTypeMember typeMember = (CodeTypeMember) obj;
                        IncorporateComments(typeMember);
                        typeDecl.Members.Add(typeMember);
                    }
                }

                if (constructor.Statements.Count > 0)
                {
                    CodeDomUtils.SetMemberAttributes(constructor, MemberAttributes.Public);
                    typeDecl.Members.Insert(0, constructor);
                }
            }

            if (_comments.Count > 0)
            {
                CodeSnippetTypeMember snippetMember = new CodeSnippetTypeMember(string.Empty);

                foreach (CodeCommentStatement commentStatement in _comments.Values)
                {
                    snippetMember.Comments.Add(commentStatement);
                }

                _comments.Clear();
                typeDecl.Members.Add(snippetMember);
            }

            Debug.Assert(_comments.Count == 0);
            Debug.Assert(_defaultAttribute == null);
            Debug.Assert(_elseLine == null);
            return typeDecl;
        }

        private object ParseReduction()
        {
            RuleConstants rule = (RuleConstants) _parser.ReductionRule.Index;

            switch (rule)
            {
                case RuleConstants.NLNewLine:
                    return ParseNLNewLine();
                case RuleConstants.NLNewLine2:
                    return ParseNLNewLine2();
                case RuleConstants.Program:
                    return ParseProgram();
                case RuleConstants.ClassDeclClassEndClass:
                    return ParseClassDeclClassEndClass();
                case RuleConstants.MemberDeclList:
                    return ParseMemberDeclList();
                case RuleConstants.MemberDeclList2:
                    return null; // ParseMemberDeclList2();
                case RuleConstants.MemberDecl:
                    return null; // ParseMemberDecl();
                case RuleConstants.MemberDecl2:
                    return null; // ParseMemberDecl2();
                case RuleConstants.MemberDecl3:
                    return null; // ParseMemberDecl3();
                case RuleConstants.MemberDecl4:
                    return null; // ParseMemberDecl4();
                case RuleConstants.MemberDecl5:
                    return null; // ParseMemberDecl5();
                case RuleConstants.MemberDecl6:
                    return null; // ParseMemberDecl6();
                case RuleConstants.FieldDeclPrivate:
                    return ParseFieldDeclPrivate();
                case RuleConstants.FieldDeclPublic:
                    return ParseFieldDeclPublic();
                case RuleConstants.FieldNameLParanRParan:
                    return ParseFieldNameLParanRParan();
                case RuleConstants.FieldName:
                    return null; // ParseFieldName();
                case RuleConstants.FieldIDID:
                    return ParseFieldIDID();
                case RuleConstants.FieldIDDefault:
                    return ParseFieldIDDefault();
                case RuleConstants.FieldIDErase:
                    return ParseFieldIDErase();
                case RuleConstants.FieldIDError:
                    return ParseFieldIDError();
                case RuleConstants.FieldIDExplicit:
                    return ParseFieldIDExplicit();
                case RuleConstants.FieldIDStep:
                    return ParseFieldIDStep();
                case RuleConstants.VarDeclDim:
                    return ParseVarDeclDim();
                case RuleConstants.VarNameLParanRParan:
                    return ParseVarNameLParanRParan();
                case RuleConstants.VarName:
                    return null; // ParseVarName();
                case RuleConstants.OtherVarsOptComma:
                    return ParseOtherVarsOptComma();
                case RuleConstants.OtherVarsOpt:
                    return null; // ParseOtherVarsOpt();
                case RuleConstants.ArrayRankListComma:
                    return ParseArrayRankListComma();
                case RuleConstants.ArrayRankList:
                    return null; // ParseArrayRankList();
                case RuleConstants.ArrayRankList2:
                    return null; // ParseArrayRankList2();
                case RuleConstants.ConstDeclConst:
                    return ParseConstDeclConst();
                case RuleConstants.ConstListEqComma:
                    return ParseConstListEqComma();
                case RuleConstants.ConstListEq:
                    return ParseConstListEq();
                case RuleConstants.ConstExprDefLParanRParan:
                    return ParseConstExprDefLParanRParan();
                case RuleConstants.ConstExprDefMinus:
                    return ParseConstExprDefMinus();
                case RuleConstants.ConstExprDefPlus:
                    return ParseConstExprDefPlus();
                case RuleConstants.ConstExprDef:
                    return null; // ParseConstExprDef();
                case RuleConstants.SubDeclSubEndSub:
                    return ParseSubDeclSubEndSub();
                case RuleConstants.SubDeclSubEndSub2:
                    return ParseSubDeclSubEndSub2();
                case RuleConstants.FunctionDeclFunctionEndFunction:
                    return ParseFunctionDeclFunctionEndFunction();
                case RuleConstants.FunctionDeclFunctionEndFunction2:
                    return ParseFunctionDeclFunctionEndFunction2();
                case RuleConstants.MethodAccessOptPublicDefault:
                    return ParseMethodAccessOptPublicDefault();
                case RuleConstants.MethodAccessOpt:
                    return null; // ParseMethodAccessOpt();
                case RuleConstants.AccessModifierOptPublic:
                    return ParseAccessModifierOptPublic();
                case RuleConstants.AccessModifierOptPrivate:
                    return ParseAccessModifierOptPrivate();
                case RuleConstants.AccessModifierOpt:
                    return null; // ParseAccessModifierOpt();
                case RuleConstants.MethodArgListLParanRParan:
                    return ParseMethodArgListLParanRParan();
                case RuleConstants.MethodArgListLParanRParan2:
                    return ParseMethodArgListLParanRParan2();
                case RuleConstants.MethodArgList:
                    return null; // ParseMethodArgList();
                case RuleConstants.ArgListComma:
                    return ParseArgListComma();
                case RuleConstants.ArgList:
                    return null; // ParseArgList();
                case RuleConstants.ArgLParanRParan:
                    return ParseArgLParanRParan();
                case RuleConstants.Arg:
                    return ParseArg();
                case RuleConstants.ArgModifierOptByVal:
                    return ParseArgModifierOptByVal();
                case RuleConstants.ArgModifierOptByRef:
                    return ParseArgModifierOptByRef();
                case RuleConstants.ArgModifierOpt:
                    return null; // ParseArgModifierOpt();
                case RuleConstants.PropertyDeclPropertyEndProperty:
                    return ParsePropertyDeclPropertyEndProperty();
                case RuleConstants.PropertyAccessTypeGet:
                    return ParsePropertyAccessTypeGet();
                case RuleConstants.PropertyAccessTypeLet:
                    return ParsePropertyAccessTypeLet();
                case RuleConstants.PropertyAccessTypeSet:
                    return ParsePropertyAccessTypeSet();
                case RuleConstants.GlobalStmt:
                    return null; // ParseGlobalStmt();
                case RuleConstants.GlobalStmt2:
                    return null; // ParseGlobalStmt2();
                case RuleConstants.GlobalStmt3:
                    return null; // ParseGlobalStmt3();
                case RuleConstants.GlobalStmt4:
                    return null; // ParseGlobalStmt4();
                case RuleConstants.GlobalStmt5:
                    return null; // ParseGlobalStmt5();
                case RuleConstants.GlobalStmt6:
                    return null; // ParseGlobalStmt6();
                case RuleConstants.GlobalStmt7:
                    return null; // ParseGlobalStmt7();
                case RuleConstants.MethodStmt:
                    return null; // ParseMethodStmt();
                case RuleConstants.MethodStmt2:
                    return null; // ParseMethodStmt2();
                case RuleConstants.BlockStmt:
                    return null; // ParseBlockStmt();
                case RuleConstants.BlockStmt2:
                    return null; // ParseBlockStmt2();
                case RuleConstants.BlockStmt3:
                    return null; // ParseBlockStmt3();
                case RuleConstants.BlockStmt4:
                    return null; // ParseBlockStmt4();
                case RuleConstants.BlockStmt5:
                    return null; // ParseBlockStmt5();
                case RuleConstants.BlockStmt6:
                    return null; // ParseBlockStmt6();
                case RuleConstants.BlockStmt7:
                    return null; // ParseBlockStmt7();
                case RuleConstants.BlockStmt8:
                    return ParseBlockStmt8();
                case RuleConstants.InlineStmt:
                    return null; // ParseInlineStmt();
                case RuleConstants.InlineStmt2:
                    return null; // ParseInlineStmt2();
                case RuleConstants.InlineStmt3:
                    return null; // ParseInlineStmt3();
                case RuleConstants.InlineStmt4:
                    return null; // ParseInlineStmt4();
                case RuleConstants.InlineStmt5:
                    return null; // ParseInlineStmt5();
                case RuleConstants.InlineStmtErase:
                    return ParseInlineStmtErase();
                case RuleConstants.GlobalStmtList:
                    return ParseGlobalStmtList();
                case RuleConstants.GlobalStmtList2:
                    return null; // ParseGlobalStmtList2();
                case RuleConstants.MethodStmtList:
                    return ParseMethodStmtList();
                case RuleConstants.MethodStmtList2:
                    return null; // ParseMethodStmtList2();
                case RuleConstants.BlockStmtList:
                    return ParseBlockStmtList();
                case RuleConstants.BlockStmtList2:
                    return null; // ParseBlockStmtList2();
                case RuleConstants.OptionExplicitOptionExplicit:
                    return ParseOptionExplicitOptionExplicit();
                case RuleConstants.ErrorStmtOnErrorResumeNext:
                    return ParseErrorStmtOnErrorResumeNext();
                case RuleConstants.ErrorStmtOnErrorGoToIntLiteral:
                    return ParseErrorStmtOnErrorGoToIntLiteral();
                case RuleConstants.ExitStmtExitDo:
                    return ParseExitStmtExitDo();
                case RuleConstants.ExitStmtExitFor:
                    return ParseExitStmtExitFor();
                case RuleConstants.ExitStmtExitFunction:
                    return ParseExitStmtExitFunction();
                case RuleConstants.ExitStmtExitProperty:
                    return ParseExitStmtExitProperty();
                case RuleConstants.ExitStmtExitSub:
                    return ParseExitStmtExitSub();
                case RuleConstants.AssignStmtEq:
                    return ParseAssignStmtEq();
                case RuleConstants.AssignStmtSetEq:
                    return ParseAssignStmtSetEq();
                case RuleConstants.SubCallStmt:
                    return ParseSubCallStmt();
                case RuleConstants.SubCallStmt2:
                    return ParseSubCallStmt2();
                case RuleConstants.SubCallStmtLParanRParan:
                    return ParseSubCallStmtLParanRParan();
                case RuleConstants.SubCallStmtLParanRParan2:
                    return ParseSubCallStmtLParanRParan2();
                case RuleConstants.SubCallStmtLParanRParan3:
                    return ParseSubCallStmtLParanRParan3();
                case RuleConstants.SubCallStmtDot:
                    return ParseSubCallStmtDot();
                case RuleConstants.SubCallStmt3:
                    return ParseSubCallStmt3();
                case RuleConstants.SubCallStmtDot2:
                    return ParseSubCallStmtDot2();
                case RuleConstants.SubCallStmt4:
                    return ParseSubCallStmt4();
                case RuleConstants.SubSafeExprOpt:
                    return null; // ParseSubSafeExprOpt();
                case RuleConstants.SubSafeExprOpt2:
                    return null; // ParseSubSafeExprOpt2();
                case RuleConstants.CallStmtCall:
                    return ParseCallStmtCall();
                case RuleConstants.LeftExprDot:
                    return ParseLeftExprDot();
                case RuleConstants.LeftExpr:
                    return ParseLeftExpr();
                case RuleConstants.LeftExpr2:
                    return ParseLeftExpr2();
                case RuleConstants.LeftExpr3:
                    return null; // ParseLeftExpr3();
                case RuleConstants.LeftExpr4:
                    return null; // ParseLeftExpr4();
                case RuleConstants.LeftExprTailDot:
                    return ParseLeftExprTailDot();
                case RuleConstants.LeftExprTail:
                    return ParseLeftExprTail();
                case RuleConstants.LeftExprTail2:
                    return ParseLeftExprTail2();
                case RuleConstants.LeftExprTail3:
                    return null; // ParseLeftExprTail3();
                case RuleConstants.QualifiedIDIDDot:
                    return ParseQualifiedIDIDDot();
                case RuleConstants.QualifiedIDDotIDDot:
                    return ParseQualifiedIDDotIDDot();
                case RuleConstants.QualifiedIDID:
                    return ParseQualifiedIDID();
                case RuleConstants.QualifiedIDDotID:
                    return ParseQualifiedIDDotID();
                case RuleConstants.QualifiedIDTailIDDot:
                    return ParseQualifiedIDTailIDDot();
                case RuleConstants.QualifiedIDTailID:
                    return ParseQualifiedIDTailID();
                case RuleConstants.QualifiedIDTail:
                    return null; // ParseQualifiedIDTail();
                case RuleConstants.KeywordID:
                    return null; // ParseKeywordID();
                case RuleConstants.KeywordIDAnd:
                    return ParseKeywordIDAnd();
                case RuleConstants.KeywordIDByRef:
                    return ParseKeywordIDByRef();
                case RuleConstants.KeywordIDByVal:
                    return ParseKeywordIDByVal();
                case RuleConstants.KeywordIDCall:
                    return ParseKeywordIDCall();
                case RuleConstants.KeywordIDCase:
                    return ParseKeywordIDCase();
                case RuleConstants.KeywordIDClass:
                    return ParseKeywordIDClass();
                case RuleConstants.KeywordIDConst:
                    return ParseKeywordIDConst();
                case RuleConstants.KeywordIDDim:
                    return ParseKeywordIDDim();
                case RuleConstants.KeywordIDDo:
                    return ParseKeywordIDDo();
                case RuleConstants.KeywordIDEach:
                    return ParseKeywordIDEach();
                case RuleConstants.KeywordIDElse:
                    return ParseKeywordIDElse();
                case RuleConstants.KeywordIDElseIf:
                    return ParseKeywordIDElseIf();
                case RuleConstants.KeywordIDEmpty:
                    return ParseKeywordIDEmpty();
                case RuleConstants.KeywordIDEnd:
                    return ParseKeywordIDEnd();
                case RuleConstants.KeywordIDEqv:
                    return ParseKeywordIDEqv();
                case RuleConstants.KeywordIDExit:
                    return ParseKeywordIDExit();
                case RuleConstants.KeywordIDFalse:
                    return ParseKeywordIDFalse();
                case RuleConstants.KeywordIDFor:
                    return ParseKeywordIDFor();
                case RuleConstants.KeywordIDFunction:
                    return ParseKeywordIDFunction();
                case RuleConstants.KeywordIDGet:
                    return ParseKeywordIDGet();
                case RuleConstants.KeywordIDGoTo:
                    return ParseKeywordIDGoTo();
                case RuleConstants.KeywordIDIf:
                    return ParseKeywordIDIf();
                case RuleConstants.KeywordIDImp:
                    return ParseKeywordIDImp();
                case RuleConstants.KeywordIDIn:
                    return ParseKeywordIDIn();
                case RuleConstants.KeywordIDIs:
                    return ParseKeywordIDIs();
                case RuleConstants.KeywordIDLet:
                    return ParseKeywordIDLet();
                case RuleConstants.KeywordIDLoop:
                    return ParseKeywordIDLoop();
                case RuleConstants.KeywordIDMod:
                    return ParseKeywordIDMod();
                case RuleConstants.KeywordIDNew:
                    return ParseKeywordIDNew();
                case RuleConstants.KeywordIDNext:
                    return ParseKeywordIDNext();
                case RuleConstants.KeywordIDNot:
                    return ParseKeywordIDNot();
                case RuleConstants.KeywordIDNothing:
                    return ParseKeywordIDNothing();
                case RuleConstants.KeywordIDNull:
                    return ParseKeywordIDNull();
                case RuleConstants.KeywordIDOn:
                    return ParseKeywordIDOn();
                case RuleConstants.KeywordIDOption:
                    return ParseKeywordIDOption();
                case RuleConstants.KeywordIDOr:
                    return ParseKeywordIDOr();
                case RuleConstants.KeywordIDPreserve:
                    return ParseKeywordIDPreserve();
                case RuleConstants.KeywordIDPrivate:
                    return ParseKeywordIDPrivate();
                case RuleConstants.KeywordIDPublic:
                    return ParseKeywordIDPublic();
                case RuleConstants.KeywordIDRedim:
                    return ParseKeywordIDRedim();
                case RuleConstants.KeywordIDResume:
                    return ParseKeywordIDResume();
                case RuleConstants.KeywordIDSelect:
                    return ParseKeywordIDSelect();
                case RuleConstants.KeywordIDSet:
                    return ParseKeywordIDSet();
                case RuleConstants.KeywordIDSub:
                    return ParseKeywordIDSub();
                case RuleConstants.KeywordIDThen:
                    return ParseKeywordIDThen();
                case RuleConstants.KeywordIDTo:
                    return ParseKeywordIDTo();
                case RuleConstants.KeywordIDTrue:
                    return ParseKeywordIDTrue();
                case RuleConstants.KeywordIDUntil:
                    return ParseKeywordIDUntil();
                case RuleConstants.KeywordIDWEnd:
                    return ParseKeywordIDWEnd();
                case RuleConstants.KeywordIDWhile:
                    return ParseKeywordIDWhile();
                case RuleConstants.KeywordIDWith:
                    return ParseKeywordIDWith();
                case RuleConstants.KeywordIDXor:
                    return ParseKeywordIDXor();
                case RuleConstants.SafeKeywordIDDefault:
                    return ParseSafeKeywordIDDefault();
                case RuleConstants.SafeKeywordIDErase:
                    return ParseSafeKeywordIDErase();
                case RuleConstants.SafeKeywordIDError:
                    return ParseSafeKeywordIDError();
                case RuleConstants.SafeKeywordIDExplicit:
                    return ParseSafeKeywordIDExplicit();
                case RuleConstants.SafeKeywordIDProperty:
                    return ParseSafeKeywordIDProperty();
                case RuleConstants.SafeKeywordIDStep:
                    return ParseSafeKeywordIDStep();
                case RuleConstants.ExtendedID:
                    return null; // ParseExtendedID();
                case RuleConstants.ExtendedIDID:
                    return ParseExtendedIDID();
                case RuleConstants.IndexOrParamsList:
                    return ParseIndexOrParamsList();
                case RuleConstants.IndexOrParamsList2:
                    return null; // ParseIndexOrParamsList2();
                case RuleConstants.IndexOrParamsLParanRParan:
                    return ParseIndexOrParamsLParanRParan();
                case RuleConstants.IndexOrParamsLParanRParan2:
                    return ParseIndexOrParamsLParanRParan2();
                case RuleConstants.IndexOrParamsLParanRParan3:
                    return ParseIndexOrParamsLParanRParan3();
                case RuleConstants.IndexOrParamsLParanRParan4:
                    return ParseIndexOrParamsLParanRParan4();
                case RuleConstants.IndexOrParamsListDot:
                    return ParseIndexOrParamsListDot();
                case RuleConstants.IndexOrParamsListDot2:
                    return null; // ParseIndexOrParamsListDot2();
                case RuleConstants.IndexOrParamsDotLParanRParanDot:
                    return ParseIndexOrParamsDotLParanRParanDot();
                case RuleConstants.IndexOrParamsDotLParanRParanDot2:
                    return ParseIndexOrParamsDotLParanRParanDot2();
                case RuleConstants.IndexOrParamsDotLParanRParanDot3:
                    return ParseIndexOrParamsDotLParanRParanDot3();
                case RuleConstants.IndexOrParamsDotLParanRParanDot4:
                    return ParseIndexOrParamsDotLParanRParanDot4();
                case RuleConstants.CommaExprListComma:
                    return ParseCommaExprListComma();
                case RuleConstants.CommaExprListComma2:
                    return ParseCommaExprListComma2();
                case RuleConstants.CommaExprListComma3:
                    return ParseCommaExprListComma3();
                case RuleConstants.CommaExprListComma4:
                    return ParseCommaExprListComma4();
                case RuleConstants.RedimStmtRedim:
                    return ParseRedimStmtRedim();
                case RuleConstants.RedimStmtRedimPreserve:
                    return ParseRedimStmtRedimPreserve();
                case RuleConstants.RedimDeclListComma:
                    return ParseRedimDeclListComma();
                case RuleConstants.RedimDeclList:
                    return null; // ParseRedimDeclList();
                case RuleConstants.RedimDeclLParanRParan:
                    return ParseRedimDeclLParanRParan();
                case RuleConstants.IfStmtIfThenEndIf:
                    return ParseIfStmtIfThenEndIf();
                case RuleConstants.IfStmtIfThen:
                    return ParseIfStmtIfThen();
                case RuleConstants.ElseStmtListElseIfThen:
                    return ParseElseStmtListElseIfThen();
                case RuleConstants.ElseStmtListElseIfThen2:
                    return ParseElseStmtListElseIfThen2();
                case RuleConstants.ElseStmtListElse:
                    return ParseElseStmtListElse();
                case RuleConstants.ElseStmtListElse2:
                    return ParseElseStmtListElse2();
                case RuleConstants.ElseStmtList:
                    return null; // ParseElseStmtList();
                case RuleConstants.ElseOptElse:
                    return ParseElseOptElse();
                case RuleConstants.ElseOpt:
                    return null; // ParseElseOpt();
                case RuleConstants.EndIfOptEndIf:
                    return ParseEndIfOptEndIf();
                case RuleConstants.EndIfOpt:
                    return null; // ParseEndIfOpt();
                case RuleConstants.WithStmtWithEndWith:
                    return ParseWithStmtWithEndWith();
                case RuleConstants.LoopStmtDoLoop:
                    return ParseLoopStmtDoLoop();
                case RuleConstants.LoopStmtDoLoop2:
                    return ParseLoopStmtDoLoop2();
                case RuleConstants.LoopStmtDoLoop3:
                    return ParseLoopStmtDoLoop3();
                case RuleConstants.LoopStmtWhileWEnd:
                    return ParseLoopStmtWhileWEnd();
                case RuleConstants.LoopTypeWhile:
                    return ParseLoopTypeWhile();
                case RuleConstants.LoopTypeUntil:
                    return ParseLoopTypeUntil();
                case RuleConstants.ForStmtForEqToNext:
                    return ParseForStmtForEqToNext();
                case RuleConstants.ForStmtForEachInNext:
                    return ParseForStmtForEachInNext();
                case RuleConstants.StepOptStep:
                    return ParseStepOptStep();
                case RuleConstants.StepOpt:
                    return null; // ParseStepOpt();
                case RuleConstants.SelectStmtSelectCaseEndSelect:
                    return ParseSelectStmtSelectCaseEndSelect();
                case RuleConstants.CaseStmtListCase:
                    return ParseCaseStmtListCase();
                case RuleConstants.CaseStmtListCaseElse:
                    return ParseCaseStmtListCaseElse();
                case RuleConstants.CaseStmtList:
                    return null; // ParseCaseStmtList();
                case RuleConstants.NLOpt:
                    return null; // ParseNLOpt();
                case RuleConstants.NLOpt2:
                    return null; // ParseNLOpt2();
                case RuleConstants.ExprListComma:
                    return ParseExprListComma();
                case RuleConstants.ExprList:
                    return null; // ParseExprList();
                case RuleConstants.SubSafeExpr:
                    return null; // ParseSubSafeExpr();
                case RuleConstants.SubSafeImpExprImp:
                    return ParseSubSafeImpExprImp();
                case RuleConstants.SubSafeImpExpr:
                    return null; // ParseSubSafeImpExpr();
                case RuleConstants.SubSafeEqvExprEqv:
                    return ParseSubSafeEqvExprEqv();
                case RuleConstants.SubSafeEqvExpr:
                    return null; // ParseSubSafeEqvExpr();
                case RuleConstants.SubSafeXorExprXor:
                    return ParseSubSafeXorExprXor();
                case RuleConstants.SubSafeXorExpr:
                    return null; // ParseSubSafeXorExpr();
                case RuleConstants.SubSafeOrExprOr:
                    return ParseSubSafeOrExprOr();
                case RuleConstants.SubSafeOrExpr:
                    return null; // ParseSubSafeOrExpr();
                case RuleConstants.SubSafeAndExprAnd:
                    return ParseSubSafeAndExprAnd();
                case RuleConstants.SubSafeAndExpr:
                    return null; // ParseSubSafeAndExpr();
                case RuleConstants.SubSafeNotExprNot:
                    return ParseSubSafeNotExprNot();
                case RuleConstants.SubSafeNotExpr:
                    return null; // ParseSubSafeNotExpr();
                case RuleConstants.SubSafeCompareExprIs:
                    return ParseSubSafeCompareExprIs();
                case RuleConstants.SubSafeCompareExprIsNot:
                    return ParseSubSafeCompareExprIsNot();
                case RuleConstants.SubSafeCompareExprGtEq:
                    return ParseSubSafeCompareExprGtEq();
                case RuleConstants.SubSafeCompareExprEqGt:
                    return ParseSubSafeCompareExprEqGt();
                case RuleConstants.SubSafeCompareExprLtEq:
                    return ParseSubSafeCompareExprLtEq();
                case RuleConstants.SubSafeCompareExprEqLt:
                    return ParseSubSafeCompareExprEqLt();
                case RuleConstants.SubSafeCompareExprGt:
                    return ParseSubSafeCompareExprGt();
                case RuleConstants.SubSafeCompareExprLt:
                    return ParseSubSafeCompareExprLt();
                case RuleConstants.SubSafeCompareExprLtGt:
                    return ParseSubSafeCompareExprLtGt();
                case RuleConstants.SubSafeCompareExprEq:
                    return ParseSubSafeCompareExprEq();
                case RuleConstants.SubSafeCompareExpr:
                    return null; // ParseSubSafeCompareExpr();
                case RuleConstants.SubSafeConcatExprAmp:
                    return ParseSubSafeConcatExprAmp();
                case RuleConstants.SubSafeConcatExpr:
                    return null; // ParseSubSafeConcatExpr();
                case RuleConstants.SubSafeAddExprPlus:
                    return ParseSubSafeAddExprPlus();
                case RuleConstants.SubSafeAddExprMinus:
                    return ParseSubSafeAddExprMinus();
                case RuleConstants.SubSafeAddExpr:
                    return null; // ParseSubSafeAddExpr();
                case RuleConstants.SubSafeModExprMod:
                    return ParseSubSafeModExprMod();
                case RuleConstants.SubSafeModExpr:
                    return null; // ParseSubSafeModExpr();
                case RuleConstants.SubSafeIntDivExprBackslash:
                    return ParseSubSafeIntDivExprBackslash();
                case RuleConstants.SubSafeIntDivExpr:
                    return null; // ParseSubSafeIntDivExpr();
                case RuleConstants.SubSafeMultExprTimes:
                    return ParseSubSafeMultExprTimes();
                case RuleConstants.SubSafeMultExprDiv:
                    return ParseSubSafeMultExprDiv();
                case RuleConstants.SubSafeMultExpr:
                    return null; // ParseSubSafeMultExpr();
                case RuleConstants.SubSafeUnaryExprMinus:
                    return ParseSubSafeUnaryExprMinus();
                case RuleConstants.SubSafeUnaryExprPlus:
                    return ParseSubSafeUnaryExprPlus();
                case RuleConstants.SubSafeUnaryExpr:
                    return null; // ParseSubSafeUnaryExpr();
                case RuleConstants.SubSafeExpExprCaret:
                    return ParseSubSafeExpExprCaret();
                case RuleConstants.SubSafeExpExpr:
                    return null; // ParseSubSafeExpExpr();
                case RuleConstants.SubSafeValue:
                    return null; // ParseSubSafeValue();
                case RuleConstants.SubSafeValue2:
                    return null; // ParseSubSafeValue2();
                case RuleConstants.SubSafeValueNew:
                    return ParseSubSafeValueNew();
                case RuleConstants.Expr:
                    return null; // ParseExpr();
                case RuleConstants.ImpExprImp:
                    return ParseImpExprImp();
                case RuleConstants.ImpExpr:
                    return null; // ParseImpExpr();
                case RuleConstants.EqvExprEqv:
                    return ParseEqvExprEqv();
                case RuleConstants.EqvExpr:
                    return null; // ParseEqvExpr();
                case RuleConstants.XorExprXor:
                    return ParseXorExprXor();
                case RuleConstants.XorExpr:
                    return null; // ParseXorExpr();
                case RuleConstants.OrExprOr:
                    return ParseOrExprOr();
                case RuleConstants.OrExpr:
                    return null; // ParseOrExpr();
                case RuleConstants.AndExprAnd:
                    return ParseAndExprAnd();
                case RuleConstants.AndExpr:
                    return null; // ParseAndExpr();
                case RuleConstants.NotExprNot:
                    return ParseNotExprNot();
                case RuleConstants.NotExpr:
                    return null; // ParseNotExpr();
                case RuleConstants.CompareExprIs:
                    return ParseCompareExprIs();
                case RuleConstants.CompareExprIsNot:
                    return ParseCompareExprIsNot();
                case RuleConstants.CompareExprGtEq:
                    return ParseCompareExprGtEq();
                case RuleConstants.CompareExprEqGt:
                    return ParseCompareExprEqGt();
                case RuleConstants.CompareExprLtEq:
                    return ParseCompareExprLtEq();
                case RuleConstants.CompareExprEqLt:
                    return ParseCompareExprEqLt();
                case RuleConstants.CompareExprGt:
                    return ParseCompareExprGt();
                case RuleConstants.CompareExprLt:
                    return ParseCompareExprLt();
                case RuleConstants.CompareExprLtGt:
                    return ParseCompareExprLtGt();
                case RuleConstants.CompareExprEq:
                    return ParseCompareExprEq();
                case RuleConstants.CompareExpr:
                    return null; // ParseCompareExpr();
                case RuleConstants.ConcatExprAmp:
                    return ParseConcatExprAmp();
                case RuleConstants.ConcatExpr:
                    return null; // ParseConcatExpr();
                case RuleConstants.AddExprPlus:
                    return ParseAddExprPlus();
                case RuleConstants.AddExprMinus:
                    return ParseAddExprMinus();
                case RuleConstants.AddExpr:
                    return null; // ParseAddExpr();
                case RuleConstants.ModExprMod:
                    return ParseModExprMod();
                case RuleConstants.ModExpr:
                    return null; // ParseModExpr();
                case RuleConstants.IntDivExprBackslash:
                    return ParseIntDivExprBackslash();
                case RuleConstants.IntDivExpr:
                    return null; // ParseIntDivExpr();
                case RuleConstants.MultExprTimes:
                    return ParseMultExprTimes();
                case RuleConstants.MultExprDiv:
                    return ParseMultExprDiv();
                case RuleConstants.MultExpr:
                    return null; // ParseMultExpr();
                case RuleConstants.UnaryExprMinus:
                    return ParseUnaryExprMinus();
                case RuleConstants.UnaryExprPlus:
                    return ParseUnaryExprPlus();
                case RuleConstants.UnaryExpr:
                    return null; // ParseUnaryExpr();
                case RuleConstants.ExpExprCaret:
                    return ParseExpExprCaret();
                case RuleConstants.ExpExpr:
                    return null; // ParseExpExpr();
                case RuleConstants.Value:
                    return null; // ParseValue();
                case RuleConstants.Value2:
                    return null; // ParseValue2();
                case RuleConstants.ValueLParanRParan:
                    return ParseValueLParanRParan();
                case RuleConstants.ValueNew:
                    return ParseValueNew();
                case RuleConstants.ConstExpr:
                    return null; // ParseConstExpr();
                case RuleConstants.ConstExpr2:
                    return null; // ParseConstExpr2();
                case RuleConstants.ConstExprFloatLiteral:
                    return ParseConstExprFloatLiteral();
                case RuleConstants.ConstExprStringLiteral:
                    return ParseConstExprStringLiteral();
                case RuleConstants.ConstExprDateLiteral:
                    return ParseConstExprDateLiteral();
                case RuleConstants.ConstExpr3:
                    return null; // ParseConstExpr3();
                case RuleConstants.BoolLiteralTrue:
                    return ParseBoolLiteralTrue();
                case RuleConstants.BoolLiteralFalse:
                    return ParseBoolLiteralFalse();
                case RuleConstants.IntLiteralIntLiteral:
                    return ParseIntLiteralIntLiteral();
                case RuleConstants.IntLiteralHexLiteral:
                    return ParseIntLiteralHexLiteral();
                case RuleConstants.IntLiteralOctLiteral:
                    return ParseIntLiteralOctLiteral();
                case RuleConstants.NothingNothing:
                    return ParseNothingNothing();
                case RuleConstants.NothingNull:
                    return ParseNothingNull();
                case RuleConstants.NothingEmpty:
                    return ParseNothingEmpty();
                default:
                    Debug.Assert(false, "Unexpected rule: " + rule);
                    return null;
            }
        }

        //=======================================

        private object ParseNLNewLine()
        {
            // <NL> ::= NewLine <NL>
            AssertReductionCount(2);
            AssertString(0, "\r\n", "\r", "\n", ":");
            AssertNewLine(1);
            return _newLinePlaceholder;
        }

        private object ParseNLNewLine2()
        {
            // <NL> ::= NewLine
            AssertReductionCount(1);
            AssertString(0, "\r\n", "\r", "\n", ":");
            return _newLinePlaceholder;
        }

        private object ParseProgram()
        {
            // <Program> ::= <NLOpt> <GlobalStmtList>
            AssertReductionCount(2);
            AssertNewLine(0);
            object item1 = GetTokenValue(1);
            AssertNullOrIs<CodeObject, List<CodeObject>>(item1);
            return item1;
        }

        private object ParseClassDeclClassEndClass()
        {
            // <ClassDecl> ::= Class <ExtendedID> <NL> <MemberDeclList> End Class <NL>
            AssertReductionCount(7);
            AssertString(0, "Class");
            AssertNewLine(2);
            AssertString(4, "End");
            AssertString(5, "Class");
            AssertNewLine(6);
            object item3 = GetTokenValue(3);
            CodeTypeDeclaration typeDecl = new CodeTypeDeclaration(ParseExtendedID(1));
            typeDecl.Attributes = 0;
            SetLastLine(typeDecl, GetTokenLine(4) - 1);

            if (_defaultAttribute != null)
            {
                Debug.Assert(_defaultAttribute.Arguments.Count == 1);
                typeDecl.CustomAttributes.Add(_defaultAttribute);
                _defaultAttribute = null;
            }

            if (item3 != null)
            {
                if (item3 is CodeVariableDeclarationStatement)
                {
                    CodeVariableDeclarationStatement variableStmt = (CodeVariableDeclarationStatement) item3;
                    CodeTypeMember typeMember = new CodeMemberField(
                        variableStmt.Type,
                        variableStmt.Name);
                    typeMember.Attributes = 0;
                    SetLine(typeMember, GetLine(variableStmt));
                    IncorporateComments(typeMember);
                    typeDecl.Members.Add(typeMember);
                }
                else if (item3 is CodeTypeMember)
                {
                    typeDecl.Members.Add((CodeTypeMember) item3);
                }
                else
                {
                    foreach (CodeObject typeMember in (IEnumerable) item3)
                    {
                        if (typeMember is CodeVariableDeclarationStatement)
                        {
                            CodeVariableDeclarationStatement variableStmt = (CodeVariableDeclarationStatement) typeMember;
                            CodeTypeMember memberField = new CodeMemberField(
                                variableStmt.Type,
                                variableStmt.Name);
                            memberField.Attributes = 0;
                            SetLine(typeMember, GetLine(variableStmt));
                            IncorporateComments(memberField);
                            typeDecl.Members.Add(memberField);
                        }
                        else
                        {
                            typeDecl.Members.Add((CodeTypeMember) typeMember);
                        }
                    }
                }
            }

            return typeDecl;
        }

        private object ParseMemberDeclList()
        {
            // <MemberDeclList> ::= <MemberDecl> <MemberDeclList>
            AssertReductionCount(2);
            return MergeObjects(0, 1);
        }

        private object ParseFieldDeclPrivate()
        {
            // <FieldDecl> ::= Private <FieldName> <OtherVarsOpt> <NL>
            AssertReductionCount(4);
            AssertString(0, "Private");
            AssertNewLine(3);
            return BuildMemberFields(GetTokenValue(1), GetTokenValue(2), MemberAttributes.Private);
        }

        private object ParseFieldDeclPublic()
        {
            // <FieldDecl> ::= Public <FieldName> <OtherVarsOpt> <NL>
            AssertReductionCount(4);
            AssertString(0, "Public");
            AssertNewLine(3);
            return BuildMemberFields(GetTokenValue(1), GetTokenValue(2), MemberAttributes.Private);
        }

        private object ParseFieldNameLParanRParan()
        {
            // <FieldName> ::= <FieldID> ( <ArrayRankList> )
            AssertReductionCount(4);
            AssertString(1, "(");
            AssertString(3, ")");
            object item2 = GetTokenValue(2);
            CodeMemberField memberField;

            if (item2 == null)
            {
                memberField = new CodeMemberField(
                    new CodeTypeReference(typeof (object[])),
                    ParseExtendedID(0));
            }
            else if (item2 is CodeExpression)
            {
                memberField = new CodeMemberField(
                    new CodeTypeReference(typeof (object[])),
                    ParseExtendedID(0));
                memberField.InitExpression = new CodeArrayCreateExpression(
                    _objectTypeReference,
                    IncrementArraySize((CodeExpression) item2));
            }
            else
            {
                CodeExpressionCollection expressions = (CodeExpressionCollection) item2;
                IncrementArraySizes(expressions);
                Type type = typeof (object);
                type = (expressions.Count == 1
                            ? type.MakeArrayType()
                            : type.MakeArrayType(expressions.Count));
                CodeTypeReference typeRef = new CodeTypeReference(type);
                memberField = new CodeMemberField(typeRef, ParseExtendedID(0));
                CodeRectangularArrayCreateExpression rectanglularArrayCreateExpr = new CodeRectangularArrayCreateExpression(_objectTypeReference);
                rectanglularArrayCreateExpr.Lengths.AddRange(expressions);
                memberField.InitExpression = rectanglularArrayCreateExpr;
            }

            memberField.Attributes = 0;
            return memberField;
        }

        private object ParseFieldIDID()
        {
            // <FieldID> ::= ID
            return ParseID();
        }

        private object ParseFieldIDDefault()
        {
            // <FieldID> ::= Default
            return ParseID();
        }

        private object ParseFieldIDErase()
        {
            // <FieldID> ::= Erase
            return ParseID();
        }

        private object ParseFieldIDError()
        {
            // <FieldID> ::= Error
            return ParseID();
        }

        private object ParseFieldIDExplicit()
        {
            // <FieldID> ::= Explicit
            return ParseID();
        }

        private object ParseFieldIDStep()
        {
            // <FieldID> ::= Step
            return ParseID();
        }

        private object ParseVarDeclDim()
        {
            // <VarDecl> ::= Dim <VarName> <OtherVarsOpt> <NL>
            AssertReductionCount(4);
            AssertString(0, "Dim");
            object item1 = GetTokenValue(1);
            object item2 = GetTokenValue(2);
            CodeVariableDeclarationStatement variableStmt;

            if (item1 is CodeVariableReferenceExpression)
            {
                variableStmt = new CodeVariableDeclarationStatement(_objectTypeReference, ((CodeVariableReferenceExpression) item1).VariableName);
            }
            else
            {
                variableStmt = (CodeVariableDeclarationStatement) item1;
            }

            if (item2 == null)
            {
                return variableStmt;
            }
            else if (item2 is CodeStatement)
            {
                return new CodeStatementCollection(
                    new CodeStatement[]
                        {
                            variableStmt,
                            (CodeStatement) item2
                        });
            }
            else
            {
                CodeStatementCollection statements = (CodeStatementCollection) item2;
                statements.Insert(0, variableStmt);
                return statements;
            }
        }

        private object ParseVarNameLParanRParan()
        {
            // <VarName> ::= <ExtendedID> ( <ArrayRankList> )
            AssertReductionCount(4);
            AssertString(1, "(");
            AssertString(3, ")");
            object item2 = GetTokenValue(2);
            CodeVariableDeclarationStatement variableDeclStmt;

            if (item2 == null)
            {
                variableDeclStmt = new CodeVariableDeclarationStatement(
                    new CodeTypeReference(typeof (object[])),
                    ParseExtendedID(0));
            }
            else if (item2 is CodeExpression)
            {
                variableDeclStmt = new CodeVariableDeclarationStatement(
                    new CodeTypeReference(typeof (object[])),
                    ParseExtendedID(0));
                variableDeclStmt.InitExpression = new CodeArrayCreateExpression(
                    _objectTypeReference,
                    IncrementArraySize((CodeExpression) item2));
            }
            else
            {
                CodeExpressionCollection expressions = (CodeExpressionCollection) item2;
                IncrementArraySizes(expressions);
                Type type = typeof (object);
                type = (expressions.Count == 1
                            ? type.MakeArrayType()
                            : type.MakeArrayType(expressions.Count));
                CodeTypeReference typeRef = new CodeTypeReference(type);
                variableDeclStmt = new CodeVariableDeclarationStatement(typeRef, ParseExtendedID(0));
                CodeRectangularArrayCreateExpression rectangularArrayCreateExpr = new CodeRectangularArrayCreateExpression(_objectTypeReference);
                rectangularArrayCreateExpr.Lengths.AddRange(expressions);
                variableDeclStmt.InitExpression = rectangularArrayCreateExpr;
            }

            return variableDeclStmt;
        }

        private object ParseOtherVarsOptComma()
        {
            // <OtherVarsOpt> ::= , <VarName> <OtherVarsOpt>
            AssertReductionCount(3);
            AssertString(0, ",");
            object item1 = GetTokenValue(1);
            object item2 = GetTokenValue(2);
            CodeStatement statement;

            if (item1 is CodeVariableReferenceExpression)
            {
                statement = new CodeVariableDeclarationStatement(_objectTypeReference, ((CodeVariableReferenceExpression) item1).VariableName);
            }
            else
            {
                statement = (CodeVariableDeclarationStatement) item1;
            }

            if (item2 == null)
            {
                return statement;
            }
            else if (item2 is CodeStatement)
            {
                return new CodeStatementCollection(
                    new CodeStatement[]
                        {
                            statement,
                            (CodeStatement) item2
                        });
            }
            else
            {
                CodeStatementCollection statements = (CodeStatementCollection) item2;
                statements.Insert(0, statement);
                return statements;
            }
        }

        private object ParseArrayRankListComma()
        {
            // <ArrayRankList> ::= <IntLiteral> , <ArrayRankList>
            AssertReductionCount(3);
            AssertString(1, ",");
            return MergeExpressions(0, 2);
        }

        private object ParseConstDeclConst()
        {
            // <ConstDecl> ::= <AccessModifierOpt> Const <ConstList> <NL>
            AssertReductionCount(4);
            AssertString(1, "Const");
            AssertNewLine(3);
            object item0 = GetTokenValue(0);
            object item2 = GetTokenValue(2);

            if (item0 == null)
            {
                AssertIs<CodeMemberField, List<CodeObject>>(item2);
            }
            else
            {
                MemberAttributes attribute = (MemberAttributes) item0;

                if (item2 is CodeMemberField)
                {
                    CodeMemberField memberField = (CodeMemberField) item2;
                    CodeDomUtils.SetMemberAttributes(memberField, attribute);
                }
                else
                {
                    foreach (CodeMemberField memberField in (IEnumerable) item2)
                    {
                        CodeDomUtils.SetMemberAttributes(memberField, attribute);
                    }
                }
            }

            return item2;
        }

        private object ParseConstListEqComma()
        {
            // <ConstList> ::= <ExtendedID> = <ConstExprDef> , <ConstList>
            AssertReductionCount(5);
            AssertString(1, "=");
            AssertString(3, ",");
            object item4 = GetTokenValue(4);
            CodeMemberField memberField = new CodeMemberField(_objectTypeReference, ParseExtendedID(0));
            memberField.Attributes = 0;
            CodeDomUtils.SetMemberAttributes(memberField, MemberAttributes.Const);
            memberField.InitExpression = (CodeExpression) GetTokenValue(2);

            if (item4 is CodeObject)
            {
                return new List<CodeObject>(
                    new CodeObject[]
                        {
                            memberField,
                            (CodeMemberField) item4
                        });
            }
            else
            {
                List<CodeObject> objects = (List<CodeObject>) item4;
                objects.Insert(0, memberField);
                return objects;
            }
        }

        private object ParseConstListEq()
        {
            // <ConstList> ::= <ExtendedID> = <ConstExprDef>
            AssertReductionCount(3);
            AssertString(1, "=");
            CodeMemberField memberField = new CodeMemberField(_objectTypeReference, ParseExtendedID(0));
            memberField.Attributes = 0;
            CodeDomUtils.SetMemberAttributes(memberField, MemberAttributes.Const);
            memberField.InitExpression = (CodeExpression) GetTokenValue(2);
            return memberField;
        }

        private object ParseConstExprDefLParanRParan()
        {
            // <ConstExprDef> ::= ( <ConstExprDef> )
            AssertReductionCount(3);
            AssertString(0, "(");
            AssertString(2, ")");
            object item1 = GetTokenValue(1);
            AssertIs<CodeExpression>(item1);
            return item1;
        }

        private object ParseConstExprDefMinus()
        {
            // <ConstExprDef> ::= - <ConstExprDef>
            return ParseUnaryExprMinus();
        }

        private object ParseConstExprDefPlus()
        {
            // <ConstExprDef> ::= + <ConstExprDef>
            return ParseUnaryExprPlus();
        }

        private object ParseSubDeclSubEndSub()
        {
            // <SubDecl> ::= <MethodAccessOpt> Sub <ExtendedID> <MethodArgList> <NL> <MethodStmtList> End Sub <NL>
            AssertReductionCount(9);
            AssertString(1, "Sub");
            AssertNewLine(4);
            AssertString(6, "End");
            AssertString(7, "Sub");
            AssertNewLine(8);
            return BuildMemberMethod(
                GetTokenValue(0),
                GetTokenValue(2),
                GetTokenValue(3),
                GetTokenValue(5),
                GetTokenLine(6));
        }

        private object ParseSubDeclSubEndSub2()
        {
            // <SubDecl> ::= <MethodAccessOpt> Sub <ExtendedID> <MethodArgList> <InlineStmt> End Sub <NL>
            AssertReductionCount(8);
            AssertString(1, "Sub");
            AssertString(5, "End");
            AssertString(6, "Sub");
            AssertNewLine(7);
            return BuildMemberMethod(
                GetTokenValue(0),
                GetTokenValue(2),
                GetTokenValue(3),
                GetTokenValue(4),
                GetTokenLine(5));
        }

        private object ParseFunctionDeclFunctionEndFunction()
        {
            // <FunctionDecl> ::= <MethodAccessOpt> Function <ExtendedID> <MethodArgList> <NL> <MethodStmtList> End Function <NL>
            AssertReductionCount(9);
            AssertString(1, "Function");
            AssertNewLine(4);
            AssertString(6, "End");
            AssertString(7, "Function");
            AssertNewLine(8);
            return BuildMemberMethodFunction(
                GetTokenValue(0),
                GetTokenValue(2),
                GetTokenValue(3),
                GetTokenValue(5),
                GetTokenLine(6));
        }

        private object ParseFunctionDeclFunctionEndFunction2()
        {
            // <FunctionDecl> ::= <MethodAccessOpt> Function <ExtendedID> <MethodArgList> <InlineStmt> End Function <NL>
            AssertReductionCount(8);
            AssertString(1, "Function");
            AssertString(5, "End");
            AssertString(6, "Function");
            AssertNewLine(7);
            return BuildMemberMethodFunction(
                GetTokenValue(0),
                GetTokenValue(2),
                GetTokenValue(3),
                GetTokenValue(4),
                GetTokenLine(5));
        }

        private object ParseMethodAccessOptPublicDefault()
        {
            // <MethodAccessOpt> ::= Public Default
            AssertReductionCount(2);
            AssertString(0, "Public");
            AssertString(1, "Default");
            _defaultAttribute = new CodeAttributeDeclaration(
                new CodeTypeReference(typeof (DefaultMemberAttribute)));
            return MemberAttributes.Public;
        }

        private object ParseAccessModifierOptPublic()
        {
            // <AccessModifierOpt> ::= Public
            AssertReductionCount(1);
            AssertString(0, "Public");
            return MemberAttributes.Public;
        }

        private object ParseAccessModifierOptPrivate()
        {
            // <AccessModifierOpt> ::= Private
            AssertReductionCount(1);
            AssertString(0, "Private");
            return MemberAttributes.Private;
        }

        private object ParseMethodArgListLParanRParan()
        {
            // <MethodArgList> ::= ( <ArgList> )
            AssertReductionCount(3);
            AssertString(0, "(");
            AssertString(2, ")");
            object item1 = GetTokenValue(1);
            AssertNullOrIs<CodeParameterDeclarationExpression, CodeParameterDeclarationExpressionCollection>(item1);
            return item1;
        }

        private object ParseMethodArgListLParanRParan2()
        {
            // <MethodArgList> ::= ( )
            AssertReductionCount(2);
            AssertString(0, "(");
            AssertString(1, ")");
            return null;
        }

        private object ParseArgListComma()
        {
            // <ArgList> ::= <Arg> , <ArgList>
            AssertReductionCount(3);
            AssertString(1, ",");
            return MergeParameterDeclarationExpressions(0, 2);
        }

        private object ParseArgLParanRParan()
        {
            // <Arg> ::= <ArgModifierOpt> <ExtendedID> ( )
            AssertReductionCount(4);
            AssertString(2, "(");
            AssertString(3, ")");
            object item0 = GetTokenValue(0);
            CodeParameterDeclarationExpression parameterDecl = new CodeParameterDeclarationExpression(new CodeTypeReference(typeof (object[])), ParseExtendedID(1));

            if (item0 != null)
            {
                parameterDecl.Direction = (FieldDirection) item0;
            }

            return parameterDecl;
        }

        private object ParseArg()
        {
            // <Arg> ::= <ArgModifierOpt> <ExtendedID>
            AssertReductionCount(2);
            object item0 = GetTokenValue(0);
            CodeParameterDeclarationExpression parameter = new CodeParameterDeclarationExpression(_objectTypeReference, ParseExtendedID(1));

            if (item0 != null)
            {
                parameter.Direction = (FieldDirection) item0;
            }

            return parameter;
        }

        private object ParseArgModifierOptByVal()
        {
            // <ArgModifierOpt> ::= ByVal
            AssertReductionCount(1);
            AssertString(0, "ByVal");
            return FieldDirection.In;
        }

        private object ParseArgModifierOptByRef()
        {
            // <ArgModifierOpt> ::= ByRef
            AssertReductionCount(1);
            AssertString(0, "ByRef");
            return FieldDirection.Ref;
        }

        private object ParsePropertyDeclPropertyEndProperty()
        {
            // <PropertyDecl> ::= <MethodAccessOpt> Property <PropertyAccessType> <ExtendedID> <MethodArgList> <NL> <MethodStmtList> End Property <NL>
            AssertReductionCount(10);
            AssertString(1, "Property");
            AssertNewLine(5);
            AssertString(7, "End");
            AssertString(8, "Property");
            AssertNewLine(9);
            object item0 = GetTokenValue(0);
            string name = ParseExtendedID(3);
            CodeMemberProperty memberProperty = new CodeMemberProperty();
            SetLastLine(memberProperty, GetTokenLine(7) - 1);
            memberProperty.Type = _objectTypeReference;
            memberProperty.Name = name;

            if (_defaultAttribute != null && _defaultAttribute.Arguments.Count == 0)
            {
                _defaultAttribute.Arguments.Add(
                    new CodeAttributeArgument(
                        new CodePrimitiveExpression(name)));
            }

            if (item0 == null)
            {
                memberProperty.Attributes = 0;
            }
            else
            {
                CodeDomUtils.SetMemberAttributes(memberProperty, (MemberAttributes) item0);
            }

            string accessType = (string) GetTokenValue(2);
            AppendParameterDeclarationExpressions(memberProperty.Parameters, 4);

            if (StringUtils.CaseInsensitiveEquals(accessType, "Get"))
            {
                AppendStatements(memberProperty.GetStatements, 6);
                memberProperty.HasGet = true;
            }
            else if (StringUtils.CaseInsensitiveEquals(accessType, "Let") || StringUtils.CaseInsensitiveEquals(accessType, "Set"))
            {
                AppendStatements(memberProperty.SetStatements, 6);
                memberProperty.HasSet = true;
            }
            else
            {
                Debug.Assert(false, "Unexpected access type: " + accessType);
            }

            return memberProperty;
        }

        private object ParsePropertyAccessTypeGet()
        {
            // <PropertyAccessType> ::= Get
            AssertReductionCount(1);
            AssertString(0, "Get");
            return GetTokenText(0);
        }

        private object ParsePropertyAccessTypeLet()
        {
            // <PropertyAccessType> ::= Let
            AssertReductionCount(1);
            AssertString(0, "Let");
            return GetTokenText(0);
        }

        private object ParsePropertyAccessTypeSet()
        {
            // <PropertyAccessType> ::= Set
            AssertReductionCount(1);
            AssertString(0, "Set");
            return GetTokenText(0);
        }

        private object ParseBlockStmt8()
        {
            // <BlockStmt> ::= <InlineStmt> <NL>
            AssertReductionCount(2);
            AssertNewLine(1);
            object item0 = GetTokenValue(0);
            AssertNullOrIs<CodeStatement>(item0);
            return item0;
        }

        private object ParseInlineStmtErase()
        {
            // <InlineStmt> ::= Erase <ExtendedID>
            AssertReductionCount(2);
            AssertString(0, "Erase");
            return new CodeAssignStatement(
                (CodeExpression) GetTokenValue(1),
                new CodePrimitiveExpression(null));
        }

        private object ParseGlobalStmtList()
        {
            // <GlobalStmtList> ::= <GlobalStmt> <GlobalStmtList>
            AssertReductionCount(2);
            return MergeObjects(0, 1);
        }

        private object ParseMethodStmtList()
        {
            // <MethodStmtList> ::= <MethodStmt> <MethodStmtList>
            AssertReductionCount(2);
            return MergeStatements(0, 1);
        }

        private object ParseBlockStmtList()
        {
            // <BlockStmtList> ::= <BlockStmt> <BlockStmtList>
            AssertReductionCount(2);
            return MergeStatements(0, 1);
        }

        private object ParseOptionExplicitOptionExplicit()
        {
            // <OptionExplicit> ::= Option Explicit <NL>
            AssertReductionCount(3);
            AssertString(0, "Option");
            AssertString(1, "Explicit");
            AssertNewLine(2);
            return null;
        }

        private object ParseErrorStmtOnErrorResumeNext()
        {
            // <ErrorStmt> ::= On Error Resume Next
            AssertReductionCount(4);
            AssertString(0, "On");
            AssertString(1, "Error");
            AssertString(2, "Resume");
            AssertString(3, "Next");
            return new CodeOnErrorStatement(true);
        }

        private object ParseErrorStmtOnErrorGoToIntLiteral()
        {
            // <ErrorStmt> ::= On Error GoTo IntLiteral
            AssertReductionCount(4);
            AssertString(0, "On");
            AssertString(1, "Error");
            AssertString(2, "GoTo");
            AssertString(3, "0");
            return new CodeOnErrorStatement(false);
        }

        private object ParseExitStmtExitDo()
        {
            // <ExitStmt> ::= Exit Do
            AssertReductionCount(2);
            AssertString(0, "Exit");
            AssertString(1, "Do");
            return new CodeExitDoStatement();
        }

        private object ParseExitStmtExitFor()
        {
            // <ExitStmt> ::= Exit For
            AssertReductionCount(2);
            AssertString(0, "Exit");
            AssertString(1, "For");
            return new CodeExitForStatement();
        }

        private object ParseExitStmtExitFunction()
        {
            // <ExitStmt> ::= Exit Function
            AssertReductionCount(2);
            AssertString(0, "Exit");
            AssertString(1, "Function");
            return new CodeMethodReturnStatement();
        }

        private object ParseExitStmtExitProperty()
        {
            // <ExitStmt> ::= Exit Property
            AssertReductionCount(2);
            AssertString(0, "Exit");
            AssertString(1, "Property");
            return new CodeMethodReturnStatement();
        }

        private object ParseExitStmtExitSub()
        {
            // <ExitStmt> ::= Exit Sub
            AssertReductionCount(2);
            AssertString(0, "Exit");
            AssertString(1, "Sub");
            return new CodeMethodReturnStatement();
        }

        private object ParseAssignStmtEq()
        {
            // <AssignStmt> ::= <LeftExpr> = <Expr>
            AssertReductionCount(3);
            AssertString(1, "=");
            return new CodeAssignStatement(
                (CodeExpression) GetTokenValue(0),
                (CodeExpression) GetTokenValue(2));
        }

        private object ParseAssignStmtSetEq()
        {
            // <AssignStmt> ::= Set <LeftExpr> = <Expr>
            AssertReductionCount(4);
            AssertString(0, "Set");
            AssertString(2, "=");
            return new CodeAssignStatement(
                (CodeExpression) GetTokenValue(1),
                (CodeExpression) GetTokenValue(3));
        }

        private object ParseSubCallStmt()
        {
            // <SubCallStmt> ::= <QualifiedID> <SubSafeExprOpt> <CommaExprList>
            AssertReductionCount(3);
            object item0 = GetTokenValue(0);
            CodeMethodReferenceExpression methodRefExpr = new CodeMethodReferenceExpression();

            if (item0 is CodeVariableReferenceExpression)
            {
                methodRefExpr.MethodName = ((CodeVariableReferenceExpression) item0).VariableName;
            }
            else
            {
                CodePropertyReferenceExpression propertyRefExpr = (CodePropertyReferenceExpression) item0;
                methodRefExpr.MethodName = propertyRefExpr.PropertyName;
                methodRefExpr.TargetObject = propertyRefExpr.TargetObject;
            }

            CodeMethodInvokeExpression invokeExpr = new CodeMethodInvokeExpression(methodRefExpr);
            AppendExpressions(invokeExpr.Parameters, MergeExpressions(1, 2));
            return new CodeExpressionStatement(invokeExpr);
        }

        private object ParseSubCallStmt2()
        {
            // <SubCallStmt> ::= <QualifiedID> <SubSafeExprOpt>
            AssertReductionCount(2);
            object item0 = GetTokenValue(0);
            CodeMethodReferenceExpression methodRefExpr = new CodeMethodReferenceExpression();

            if (item0 is CodeVariableReferenceExpression)
            {
                methodRefExpr.MethodName = ((CodeVariableReferenceExpression) item0).VariableName;
            }
            else
            {
                CodePropertyReferenceExpression propertyRefExpr = (CodePropertyReferenceExpression) item0;
                methodRefExpr.MethodName = propertyRefExpr.PropertyName;
                methodRefExpr.TargetObject = propertyRefExpr.TargetObject;
            }

            CodeMethodInvokeExpression invokeExpr = new CodeMethodInvokeExpression(methodRefExpr);
            AppendExpressions(invokeExpr.Parameters, 1);
            return new CodeExpressionStatement(invokeExpr);
        }

        private object ParseSubCallStmtLParanRParan()
        {
            // <SubCallStmt> ::= <QualifiedID> ( <Expr> ) <CommaExprList>
            AssertReductionCount(5);
            AssertString(1, "(");
            AssertString(3, ")");
            object item0 = GetTokenValue(0);
            CodeMethodReferenceExpression methodRefExpr = new CodeMethodReferenceExpression();

            if (item0 is CodeVariableReferenceExpression)
            {
                methodRefExpr.MethodName = ((CodeVariableReferenceExpression) item0).VariableName;
            }
            else
            {
                CodePropertyReferenceExpression propertyRefExpr = (CodePropertyReferenceExpression) GetTokenValue(0);
                methodRefExpr.MethodName = propertyRefExpr.PropertyName;
                methodRefExpr.TargetObject = propertyRefExpr.TargetObject;
            }

            CodeMethodInvokeExpression invokeExpr = new CodeMethodInvokeExpression(methodRefExpr);
            AppendExpressions(invokeExpr.Parameters, 2);
            AppendExpressions(invokeExpr.Parameters, 4);
            return new CodeExpressionStatement(invokeExpr);
        }

        private object ParseSubCallStmtLParanRParan2()
        {
            // <SubCallStmt> ::= <QualifiedID> ( <Expr> )
            AssertReductionCount(4);
            AssertString(1, "(");
            AssertString(3, ")");
            object item0 = GetTokenValue(0);
            CodeMethodReferenceExpression methodRefExpr = new CodeMethodReferenceExpression();

            if (item0 is CodeVariableReferenceExpression)
            {
                methodRefExpr.MethodName = ((CodeVariableReferenceExpression) item0).VariableName;
            }
            else
            {
                CodePropertyReferenceExpression propertyRefExpr = (CodePropertyReferenceExpression) GetTokenValue(0);
                methodRefExpr.MethodName = propertyRefExpr.PropertyName;
                methodRefExpr.TargetObject = propertyRefExpr.TargetObject;
            }

            CodeMethodInvokeExpression invokeExpr = new CodeMethodInvokeExpression(methodRefExpr);
            AppendExpressions(invokeExpr.Parameters, 2);
            return new CodeExpressionStatement(invokeExpr);
        }

        private object ParseSubCallStmtLParanRParan3()
        {
            // <SubCallStmt> ::= <QualifiedID> ( )
            AssertReductionCount(3);
            AssertString(1, "(");
            AssertString(2, ")");
            object item0 = GetTokenValue(0);
            CodeMethodReferenceExpression methodRefExpr = new CodeMethodReferenceExpression();

            if (item0 is CodeVariableReferenceExpression)
            {
                methodRefExpr.MethodName = ((CodeVariableReferenceExpression) item0).VariableName;
            }
            else
            {
                CodePropertyReferenceExpression propertyRefExpr = (CodePropertyReferenceExpression) GetTokenValue(0);
                methodRefExpr.MethodName = propertyRefExpr.PropertyName;
                methodRefExpr.TargetObject = propertyRefExpr.TargetObject;
            }

            return new CodeExpressionStatement(
                new CodeMethodInvokeExpression(methodRefExpr));
        }

        private object ParseSubCallStmtDot()
        {
            // <SubCallStmt> ::= <QualifiedID> <IndexOrParamsList> . <LeftExprTail> <SubSafeExprOpt> <CommaExprList>
            AssertReductionCount(6);
            throw new NotSupportedException("SubCallStmtDot");
        }

        private object ParseSubCallStmt3()
        {
            // <SubCallStmt> ::= <QualifiedID> <IndexOrParamsListDot> <LeftExprTail> <SubSafeExprOpt> <CommaExprList>
            AssertReductionCount(5);
            object item0 = GetTokenValue(0);
            object item2 = GetTokenValue(2);

            CodeMethodReferenceExpression methodRefExpr = new CodeMethodReferenceExpression();

            if (item0 is CodeVariableReferenceExpression)
            {
                methodRefExpr.MethodName = ((CodeVariableReferenceExpression) item0).VariableName;
            }
            else
            {
                CodePropertyReferenceExpression expr = (CodePropertyReferenceExpression) item0;
                methodRefExpr.TargetObject = expr.TargetObject;
                methodRefExpr.MethodName = expr.PropertyName;
            }

            CodeMethodInvokeExpression methodInvokeExpr = new CodeMethodInvokeExpression(methodRefExpr);
            AppendExpressions(methodInvokeExpr.Parameters, 1);

            if (item2 is CodeVariableReferenceExpression)
            {
                methodInvokeExpr = new CodeMethodInvokeExpression(
                    methodInvokeExpr,
                    ((CodeVariableReferenceExpression) item2).VariableName);
            }
            else if (item2 is CodePropertyReferenceExpression)
            {
                CodePropertyReferenceExpression propertyRefExpr = (CodePropertyReferenceExpression) item2;
                CodeMethodInvokeExpression newMethodInvokeExpr = new CodeMethodInvokeExpression(
                    propertyRefExpr.TargetObject,
                    propertyRefExpr.PropertyName);
                SetRootTarget(newMethodInvokeExpr, methodInvokeExpr);
                methodInvokeExpr = newMethodInvokeExpr;
            }

            AppendExpressions(methodInvokeExpr.Parameters, MergeExpressions(3, 4));
            return new CodeExpressionStatement(methodInvokeExpr);
        }

        private object ParseSubCallStmtDot2()
        {
            // <SubCallStmt> ::= <QualifiedID> <IndexOrParamsList> . <LeftExprTail> <SubSafeExprOpt>
            AssertReductionCount(5);
            throw new NotSupportedException("SubCallStmtDot2");
        }

        private object ParseSubCallStmt4()
        {
            // <SubCallStmt> ::= <QualifiedID> <IndexOrParamsListDot> <LeftExprTail> <SubSafeExprOpt>
            AssertReductionCount(4);
            object item0 = GetTokenValue(0);
            object item1 = GetTokenValue(1);
            object item2 = GetTokenValue(2);

            CodeMethodInvokeExpression methodInvokeExpr = item1 as CodeMethodInvokeExpression;
            CodeMethodInvokeExpression rootMethodInvokeExpr = null;

            while (methodInvokeExpr != null && methodInvokeExpr.Method.TargetObject != null)
            {
                rootMethodInvokeExpr = methodInvokeExpr;
                methodInvokeExpr = methodInvokeExpr.Method.TargetObject as CodeMethodInvokeExpression;
            }

            CodeMethodInvokeExpression newMethodInvokeExpr = new CodeMethodInvokeExpression();
            CodeExpression target = item1 as CodeExpression;

            if (methodInvokeExpr == null)
            {
                AppendExpressions(newMethodInvokeExpr.Parameters, item1);
                target = newMethodInvokeExpr;
            }
            else
            {
                newMethodInvokeExpr.Parameters.AddRange(methodInvokeExpr.Parameters);

                if (rootMethodInvokeExpr != null)
                {
                    rootMethodInvokeExpr.Method.TargetObject = newMethodInvokeExpr;
                }
            }

            CodeMethodReferenceExpression methodRefExpr = newMethodInvokeExpr.Method;

            if (item0 is CodeVariableReferenceExpression)
            {
                methodRefExpr.MethodName = ((CodeVariableReferenceExpression) item0).VariableName;
            }
            else
            {
                CodePropertyReferenceExpression propertyRefExpr = (CodePropertyReferenceExpression) item0;
                methodRefExpr.TargetObject = propertyRefExpr.TargetObject;
                methodRefExpr.MethodName = propertyRefExpr.PropertyName;
            }

            if (item2 is CodeVariableReferenceExpression)
            {
                newMethodInvokeExpr = new CodeMethodInvokeExpression(
                    target,
                    ((CodeVariableReferenceExpression) item2).VariableName);
            }
            else if (item2 is CodePropertyReferenceExpression)
            {
                CodePropertyReferenceExpression propertyRefExpr = (CodePropertyReferenceExpression) item2;
                newMethodInvokeExpr = new CodeMethodInvokeExpression(
                    propertyRefExpr.TargetObject,
                    propertyRefExpr.PropertyName);
                SetRootTarget(newMethodInvokeExpr, target);
            }
            else
            {
                newMethodInvokeExpr = (CodeMethodInvokeExpression) item2;
                SetRootTarget(newMethodInvokeExpr, target);
            }

            AppendExpressions(newMethodInvokeExpr.Parameters, 3);
            return new CodeExpressionStatement(newMethodInvokeExpr);
        }

        private object ParseCallStmtCall()
        {
            // <CallStmt> ::= Call <LeftExpr>
            AssertReductionCount(2);
            AssertString(0, "Call");
            object item1 = GetTokenValue(1);
            CodeMethodInvokeExpression expression;

            if (item1 is CodeVariableReferenceExpression)
            {
                expression = new CodeMethodInvokeExpression(
                    null,
                    ((CodeVariableReferenceExpression) item1).VariableName);
            }
            else if (item1 is CodePropertyReferenceExpression)
            {
                CodePropertyReferenceExpression propertyRefExpr = (CodePropertyReferenceExpression) item1;
                expression = new CodeMethodInvokeExpression(
                    propertyRefExpr.TargetObject,
                    propertyRefExpr.PropertyName);
            }
            else
            {
                expression = (CodeMethodInvokeExpression) item1;
            }

            return new CodeExpressionStatement(expression);
        }

        private object ParseLeftExprDot()
        {
            // <LeftExpr> ::= <QualifiedID> <IndexOrParamsList> . <LeftExprTail>
            AssertReductionCount(4);
            throw new NotSupportedException("LeftExprDot");
        }

        private object ParseLeftExpr()
        {
            // <LeftExpr> ::= <QualifiedID> <IndexOrParamsListDot> <LeftExprTail>
            AssertReductionCount(3);
            object item0 = GetTokenValue(0);
            object item1 = GetTokenValue(1);
            object item2 = GetTokenValue(2);

            CodeMethodInvokeExpression methodInvokeExpr = item1 as CodeMethodInvokeExpression;
            CodeMethodInvokeExpression rootMethodInvokeExpr = null;

            while (methodInvokeExpr != null && methodInvokeExpr.Method.TargetObject != null)
            {
                rootMethodInvokeExpr = methodInvokeExpr;
                methodInvokeExpr = methodInvokeExpr.Method.TargetObject as CodeMethodInvokeExpression;
            }

            CodeMethodInvokeExpression newMethodInvokeExpr = new CodeMethodInvokeExpression();
            CodeExpression target = item1 as CodeExpression;

            if (methodInvokeExpr == null)
            {
                AppendExpressions(newMethodInvokeExpr.Parameters, item1);
                target = newMethodInvokeExpr;
            }
            else
            {
                newMethodInvokeExpr.Parameters.AddRange(methodInvokeExpr.Parameters);

                if (rootMethodInvokeExpr != null)
                {
                    rootMethodInvokeExpr.Method.TargetObject = newMethodInvokeExpr;
                }
            }

            CodeMethodReferenceExpression methodRefExpr = newMethodInvokeExpr.Method;

            if (item0 is CodeVariableReferenceExpression)
            {
                methodRefExpr.MethodName = ((CodeVariableReferenceExpression) item0).VariableName;
            }
            else
            {
                CodePropertyReferenceExpression propertyRefExpr = (CodePropertyReferenceExpression) item0;
                methodRefExpr.TargetObject = propertyRefExpr.TargetObject;
                methodRefExpr.MethodName = propertyRefExpr.PropertyName;
            }

            if (item2 is CodeVariableReferenceExpression)
            {
                newMethodInvokeExpr = new CodeMethodInvokeExpression(
                    target,
                    ((CodeVariableReferenceExpression) item2).VariableName);
            }
            else if (item2 is CodePropertyReferenceExpression)
            {
                CodePropertyReferenceExpression propertyRefExpr = (CodePropertyReferenceExpression) item2;
                newMethodInvokeExpr = new CodeMethodInvokeExpression(
                    propertyRefExpr.TargetObject,
                    propertyRefExpr.PropertyName);
                SetRootTarget(newMethodInvokeExpr, target);
            }
            else
            {
                newMethodInvokeExpr = (CodeMethodInvokeExpression) item2;
                SetRootTarget(newMethodInvokeExpr, target);
            }

            return newMethodInvokeExpr;
        }

        private object ParseLeftExpr2()
        {
            // <LeftExpr> ::= <QualifiedID> <IndexOrParamsList>
            AssertReductionCount(2);
            object item0 = GetTokenValue(0);
            object item1 = GetTokenValue(1);

            CodeMethodInvokeExpression methodInvokeExpr = item1 as CodeMethodInvokeExpression;

            if (methodInvokeExpr != null && string.IsNullOrEmpty(methodInvokeExpr.Method.MethodName))
            {
                while (true)
                {
                    if (methodInvokeExpr.Method.TargetObject == null)
                    {
                        if (item0 is CodePropertyReferenceExpression)
                        {
                            CodePropertyReferenceExpression propertyRefExpr = (CodePropertyReferenceExpression) item0;
                            methodInvokeExpr.Method.TargetObject = propertyRefExpr.TargetObject;
                            methodInvokeExpr.Method.MethodName = propertyRefExpr.PropertyName;
                        }
                        else
                        {
                            methodInvokeExpr.Method.MethodName = ((CodeVariableReferenceExpression) item0).VariableName;
                        }

                        break;
                    }
                    else
                    {
                        methodInvokeExpr = (CodeMethodInvokeExpression) methodInvokeExpr.Method.TargetObject;
                    }
                }
            }
            else if (item0 is CodePropertyReferenceExpression)
            {
                CodePropertyReferenceExpression propertyRefExpr = (CodePropertyReferenceExpression) item0;
                methodInvokeExpr = new CodeMethodInvokeExpression(
                    propertyRefExpr.TargetObject,
                    propertyRefExpr.PropertyName);
                AppendExpressions(methodInvokeExpr.Parameters, item1);
                item1 = methodInvokeExpr;
            }
            else
            {
                methodInvokeExpr = new CodeMethodInvokeExpression(
                    null,
                    ((CodeVariableReferenceExpression) item0).VariableName);
                AppendExpressions(methodInvokeExpr.Parameters, item1);
                item1 = methodInvokeExpr;
            }

            return item1;
        }

        private object ParseLeftExprTailDot()
        {
            // <LeftExprTail> ::= <QualifiedIDTail> <IndexOrParamsList> . <LeftExprTail>
            AssertReductionCount(4);
            throw new NotSupportedException("LeftExprTailDot");
        }

        private object ParseLeftExprTail()
        {
            // <LeftExprTail> ::= <QualifiedIDTail> <IndexOrParamsListDot> <LeftExprTail>
            return ParseLeftExpr();
        }

        private object ParseLeftExprTail2()
        {
            // <LeftExprTail> ::= <QualifiedIDTail> <IndexOrParamsList>
            return ParseLeftExpr2();
        }

        private object ParseQualifiedIDIDDot()
        {
            // <QualifiedID> ::= IDDot <QualifiedIDTail>
            AssertReductionCount(2);
            object item1 = GetTokenValue(1);
            string id = GetTokenText(0);
            CodeExpression expression = new CodeVariableReferenceExpression(
                id.Substring(0, id.Length - 1));

            if (item1 is CodeVariableReferenceExpression)
            {
                return new CodePropertyReferenceExpression(
                    expression,
                    ((CodeVariableReferenceExpression) item1).VariableName);
            }
            else
            {
                CodePropertyReferenceExpression propertyRefExpr = (CodePropertyReferenceExpression) item1;
                SetRootTarget(propertyRefExpr, expression);
                return propertyRefExpr;
            }
        }

        private object ParseQualifiedIDDotIDDot()
        {
            // <QualifiedID> ::= DotIDDot <QualifiedIDTail>
            AssertReductionCount(2);
            object item1 = GetTokenValue(1);
            string id = GetTokenText(0);
            CodePropertyReferenceExpression propertyRefExpr = new CodePropertyReferenceExpression(
                CodeWithStatement.TargetPlaceholder,
                id.Substring(1, id.Length - 2));

            if (item1 is CodeVariableReferenceExpression)
            {
                return new CodePropertyReferenceExpression(
                    propertyRefExpr,
                    ((CodeVariableReferenceExpression) item1).VariableName);
            }
            else
            {
                CodePropertyReferenceExpression expression = (CodePropertyReferenceExpression) item1;
                SetRootTarget(expression, propertyRefExpr);
                return expression;
            }
        }

        private object ParseQualifiedIDID()
        {
            // <QualifiedID> ::= ID
            return ParseID();
        }

        private object ParseQualifiedIDDotID()
        {
            // <QualifiedID> ::= DotID
            AssertReductionCount(1);
            return new CodePropertyReferenceExpression(
                CodeWithStatement.TargetPlaceholder,
                (GetTokenText(0)).Substring(1));
        }

        private object ParseQualifiedIDTailIDDot()
        {
            // <QualifiedIDTail> ::= IDDot <QualifiedIDTail>
            return ParseQualifiedIDIDDot();
        }

        private object ParseQualifiedIDTailID()
        {
            // <QualifiedIDTail> ::= ID
            return ParseID();
        }

        private object ParseKeywordIDAnd()
        {
            // <KeywordID> ::= And
            return ParseID();
        }

        private object ParseKeywordIDByRef()
        {
            // <KeywordID> ::= ByRef
            return ParseID();
        }

        private object ParseKeywordIDByVal()
        {
            // <KeywordID> ::= ByVal
            return ParseID();
        }

        private object ParseKeywordIDCall()
        {
            // <KeywordID> ::= Call
            return ParseID();
        }

        private object ParseKeywordIDCase()
        {
            // <KeywordID> ::= Case
            return ParseID();
        }

        private object ParseKeywordIDClass()
        {
            // <KeywordID> ::= Class
            return ParseID();
        }

        private object ParseKeywordIDConst()
        {
            // <KeywordID> ::= Const
            return ParseID();
        }

        private object ParseKeywordIDDim()
        {
            // <KeywordID> ::= Dim
            return ParseID();
        }

        private object ParseKeywordIDDo()
        {
            // <KeywordID> ::= Do
            return ParseID();
        }

        private object ParseKeywordIDEach()
        {
            // <KeywordID> ::= Each
            return ParseID();
        }

        private object ParseKeywordIDElse()
        {
            // <KeywordID> ::= Else
            return ParseID();
        }

        private object ParseKeywordIDElseIf()
        {
            // <KeywordID> ::= ElseIf
            return ParseID();
        }

        private object ParseKeywordIDEmpty()
        {
            // <KeywordID> ::= Empty
            return ParseID();
        }

        private object ParseKeywordIDEnd()
        {
            // <KeywordID> ::= End
            return ParseID();
        }

        private object ParseKeywordIDEqv()
        {
            // <KeywordID> ::= Eqv
            return ParseID();
        }

        private object ParseKeywordIDExit()
        {
            // <KeywordID> ::= Exit
            return ParseID();
        }

        private object ParseKeywordIDFalse()
        {
            // <KeywordID> ::= False
            return ParseID();
        }

        private object ParseKeywordIDFor()
        {
            // <KeywordID> ::= For
            return ParseID();
        }

        private object ParseKeywordIDFunction()
        {
            // <KeywordID> ::= Function
            return ParseID();
        }

        private object ParseKeywordIDGet()
        {
            // <KeywordID> ::= Get
            return ParseID();
        }

        private object ParseKeywordIDGoTo()
        {
            // <KeywordID> ::= GoTo
            return ParseID();
        }

        private object ParseKeywordIDIf()
        {
            // <KeywordID> ::= If
            return ParseID();
        }

        private object ParseKeywordIDImp()
        {
            // <KeywordID> ::= Imp
            return ParseID();
        }

        private object ParseKeywordIDIn()
        {
            // <KeywordID> ::= In
            return ParseID();
        }

        private object ParseKeywordIDIs()
        {
            // <KeywordID> ::= Is
            return ParseID();
        }

        private object ParseKeywordIDLet()
        {
            // <KeywordID> ::= Let
            return ParseID();
        }

        private object ParseKeywordIDLoop()
        {
            // <KeywordID> ::= Loop
            return ParseID();
        }

        private object ParseKeywordIDMod()
        {
            // <KeywordID> ::= Mod
            return ParseID();
        }

        private object ParseKeywordIDNew()
        {
            // <KeywordID> ::= New
            return ParseID();
        }

        private object ParseKeywordIDNext()
        {
            // <KeywordID> ::= Next
            return ParseID();
        }

        private object ParseKeywordIDNot()
        {
            // <KeywordID> ::= Not
            return ParseID();
        }

        private object ParseKeywordIDNothing()
        {
            // <KeywordID> ::= Nothing
            return ParseID();
        }

        private object ParseKeywordIDNull()
        {
            // <KeywordID> ::= Null
            return ParseID();
        }

        private object ParseKeywordIDOn()
        {
            // <KeywordID> ::= On
            return ParseID();
        }

        private object ParseKeywordIDOption()
        {
            // <KeywordID> ::= Option
            return ParseID();
        }

        private object ParseKeywordIDOr()
        {
            // <KeywordID> ::= Or
            return ParseID();
        }

        private object ParseKeywordIDPreserve()
        {
            // <KeywordID> ::= Preserve
            return ParseID();
        }

        private object ParseKeywordIDPrivate()
        {
            // <KeywordID> ::= Private
            return ParseID();
        }

        private object ParseKeywordIDPublic()
        {
            // <KeywordID> ::= Public
            return ParseID();
        }

        private object ParseKeywordIDRedim()
        {
            // <KeywordID> ::= Redim
            return ParseID();
        }

        private object ParseKeywordIDResume()
        {
            // <KeywordID> ::= Resume
            return ParseID();
        }

        private object ParseKeywordIDSelect()
        {
            // <KeywordID> ::= Select
            return ParseID();
        }

        private object ParseKeywordIDSet()
        {
            // <KeywordID> ::= Set
            return ParseID();
        }

        private object ParseKeywordIDSub()
        {
            // <KeywordID> ::= Sub
            return ParseID();
        }

        private object ParseKeywordIDThen()
        {
            // <KeywordID> ::= Then
            return ParseID();
        }

        private object ParseKeywordIDTo()
        {
            // <KeywordID> ::= To
            return ParseID();
        }

        private object ParseKeywordIDTrue()
        {
            // <KeywordID> ::= True
            return ParseID();
        }

        private object ParseKeywordIDUntil()
        {
            // <KeywordID> ::= Until
            return ParseID();
        }

        private object ParseKeywordIDWEnd()
        {
            // <KeywordID> ::= WEnd
            return ParseID();
        }

        private object ParseKeywordIDWhile()
        {
            // <KeywordID> ::= While
            return ParseID();
        }

        private object ParseKeywordIDWith()
        {
            // <KeywordID> ::= With
            return ParseID();
        }

        private object ParseKeywordIDXor()
        {
            // <KeywordID> ::= Xor
            return ParseID();
        }

        private object ParseSafeKeywordIDDefault()
        {
            // <SafeKeywordID> ::= Default
            return ParseID();
        }

        private object ParseSafeKeywordIDErase()
        {
            // <SafeKeywordID> ::= Erase
            return ParseID();
        }

        private object ParseSafeKeywordIDError()
        {
            // <SafeKeywordID> ::= Error
            return ParseID();
        }

        private object ParseSafeKeywordIDExplicit()
        {
            // <SafeKeywordID> ::= Explicit
            return ParseID();
        }

        private object ParseSafeKeywordIDProperty()
        {
            // <SafeKeywordID> ::= Property
            return ParseID();
        }

        private object ParseSafeKeywordIDStep()
        {
            // <SafeKeywordID> ::= Step
            return ParseID();
        }

        private object ParseExtendedIDID()
        {
            // <ExtendedID> ::= ID
            return ParseID();
        }

        private object ParseIndexOrParamsList()
        {
            // <IndexOrParamsList> ::= <IndexOrParams> <IndexOrParamsList>
            AssertReductionCount(2);
            object item1 = GetTokenValue(1);

            CodeMethodInvokeExpression methodInvokeExpr = item1 as CodeMethodInvokeExpression;

            while (methodInvokeExpr != null && methodInvokeExpr.Method.TargetObject != null)
            {
                methodInvokeExpr = methodInvokeExpr.Method.TargetObject as CodeMethodInvokeExpression;
            }

            if (methodInvokeExpr == null)
            {
                methodInvokeExpr = new CodeMethodInvokeExpression();
                AppendExpressions(methodInvokeExpr.Parameters, item1);
                item1 = methodInvokeExpr;
            }

            CodeMethodInvokeExpression targetIndexerExpr = new CodeMethodInvokeExpression();
            AppendExpressions(targetIndexerExpr.Parameters, 0);
            methodInvokeExpr.Method.TargetObject = targetIndexerExpr;
            return item1;
        }

        private object ParseIndexOrParamsLParanRParan()
        {
            // <IndexOrParams> ::= ( <Expr> <CommaExprList> )
            AssertReductionCount(4);
            AssertString(0, "(");
            AssertString(3, ")");
            return MergeExpressions(1, 2);
        }

        private object ParseIndexOrParamsLParanRParan2()
        {
            // <IndexOrParams> ::= ( <CommaExprList> )
            AssertReductionCount(3);
            AssertString(0, "(");
            AssertString(2, ")");
            object item1 = GetTokenValue(1);
            AssertIs<CodeExpression, CodeExpressionCollection>(item1);
            return item1;
        }

        private object ParseIndexOrParamsLParanRParan3()
        {
            // <IndexOrParams> ::= ( <Expr> )
            AssertReductionCount(3);
            AssertString(0, "(");
            AssertString(2, ")");
            object item1 = GetTokenValue(1);
            AssertIs<CodeExpression>(item1);
            return item1;
        }

        private object ParseIndexOrParamsLParanRParan4()
        {
            // <IndexOrParams> ::= ( )
            AssertReductionCount(2);
            AssertString(0, "(");
            AssertString(1, ")");
            return null;
        }

        private object ParseIndexOrParamsListDot()
        {
            // <IndexOrParamsListDot> ::= <IndexOrParams> <IndexOrParamsListDot>
            return ParseIndexOrParamsList();
        }

        private object ParseIndexOrParamsDotLParanRParanDot()
        {
            // <IndexOrParamsDot> ::= ( <Expr> <CommaExprList> ).
            AssertReductionCount(4);
            AssertString(0, "(");
            AssertString(3, ").");
            return MergeExpressions(1, 2);
        }

        private object ParseIndexOrParamsDotLParanRParanDot2()
        {
            // <IndexOrParamsDot> ::= ( <CommaExprList> ).
            AssertReductionCount(3);
            throw new NotSupportedException("IndexOrParamsDotLParanRParanDot2");
        }

        private object ParseIndexOrParamsDotLParanRParanDot3()
        {
            // <IndexOrParamsDot> ::= ( <Expr> ).
            AssertReductionCount(3);
            AssertString(0, "(");
            AssertString(2, ").");
            object item1 = GetTokenValue(1);
            AssertIs<CodeExpression>(item1);
            return item1;
        }

        private object ParseIndexOrParamsDotLParanRParanDot4()
        {
            // <IndexOrParamsDot> ::= ( ).
            AssertReductionCount(2);
            AssertString(0, "(");
            AssertString(1, ").");
            return null;
        }

        private object ParseCommaExprListComma()
        {
            // <CommaExprList> ::= , <Expr> <CommaExprList>
            AssertReductionCount(3);
            AssertString(0, ",");
            return MergeExpressions(1, 2);
        }

        private object ParseCommaExprListComma2()
        {
            // <CommaExprList> ::= , <CommaExprList>
            AssertReductionCount(2);
            AssertString(0, ",");
            object item1 = GetTokenValue(1);
            //TODO: inadequate since the default value might be a value type which would be infered as nullable

            if (item1 is CodeExpression)
            {
                item1 = new CodeExpressionCollection(
                    new CodeExpression[]
                        {
                            new CodePrimitiveExpression(null),
                            (CodeExpression) item1
                        });
            }
            else
            {
                CodeExpressionCollection exprs = (CodeExpressionCollection) item1;
                exprs.Insert(0, new CodePrimitiveExpression(null));
            }

            return item1;
        }

        private object ParseCommaExprListComma3()
        {
            // <CommaExprList> ::= , <Expr>
            AssertReductionCount(2);
            AssertString(0, ",");
            object item1 = GetTokenValue(1);
            AssertIs<CodeExpression>(item1);
            return item1;
        }

        private object ParseCommaExprListComma4()
        {
            // <CommaExprList> ::= ,
            //TODO: fix grammar, VBS doesn't allow method calls ending in a comma
            AssertReductionCount(1);
            throw new NotSupportedException("ParseCommaExprListComma4");
        }

        private object ParseRedimStmtRedim()
        {
            // <RedimStmt> ::= Redim <RedimDeclList> <NL>
            AssertReductionCount(3);
            AssertString(0, "ReDim");
            AssertNewLine(2);
            object item1 = GetTokenValue(1);
            AssertIs<CodeAssignStatement, CodeStatementCollection>(item1);
            return item1;
        }

        private object ParseRedimStmtRedimPreserve()
        {
            // <RedimStmt> ::= Redim Preserve <RedimDeclList> <NL>
            AssertReductionCount(4);
            AssertString(0, "ReDim");
            AssertString(1, "Preserve");
            AssertNewLine(3);
            object item2 = GetTokenValue(2);

            if (item2 is CodeAssignStatement)
            {
                CodeAssignStatement assignStmt = (CodeAssignStatement) item2;
                CodeRectangularArrayCreateExpression rectangularArrayCreateExpr = assignStmt.Right as CodeRectangularArrayCreateExpression;
                Type type = typeof (object);
                type = (rectangularArrayCreateExpr == null || rectangularArrayCreateExpr.Lengths.Count == 1
                            ? type.MakeArrayType()
                            : type.MakeArrayType(rectangularArrayCreateExpr.Lengths.Count));
                CodeTypeReference typeRef = new CodeTypeReference(type);
                assignStmt.Right = new CodeCastExpression(
                    typeRef,
                    new CodeMethodInvokeExpression(
                        new CodeTypeReferenceExpression(new CodeTypeReference(typeof (Utils))),
                        "CopyArray",
                        new CodeCastExpression(
                            new CodeTypeReference(typeof (Array)),
                            assignStmt.Left),
                        assignStmt.Right));
            }
            else
            {
                foreach (CodeAssignStatement assignStmt in (IEnumerable) item2)
                {
                    CodeRectangularArrayCreateExpression rectangularArrayCreateExpr = assignStmt.Right as CodeRectangularArrayCreateExpression;
                    Type type = typeof (object);
                    type = (rectangularArrayCreateExpr == null || rectangularArrayCreateExpr.Lengths.Count == 1
                                ? type.MakeArrayType()
                                : type.MakeArrayType(rectangularArrayCreateExpr.Lengths.Count));
                    CodeTypeReference typeRef = new CodeTypeReference(type);
                    assignStmt.Right = new CodeCastExpression(
                        typeRef,
                        new CodeMethodInvokeExpression(
                            new CodeTypeReferenceExpression(new CodeTypeReference(typeof (Utils))),
                            "CopyArray",
                            new CodeCastExpression(
                                new CodeTypeReference(typeof (Array)),
                                assignStmt.Left),
                            assignStmt.Right));
                }
            }

            AssertIs<CodeAssignStatement, CodeStatementCollection>(item2);
            return item2;
        }

        private object ParseRedimDeclListComma()
        {
            // <RedimDeclList> ::= <RedimDecl> , <RedimDeclList>
            AssertReductionCount(3);
            AssertString(1, ",");
            return MergeStatements(0, 2);
        }

        private object ParseRedimDeclLParanRParan()
        {
            // <RedimDecl> ::= <ExtendedID> ( <ExprList> )
            AssertReductionCount(4);
            AssertString(1, "(");
            AssertString(3, ")");
            object item2 = GetTokenValue(2);
            CodeExpression expression;

            if (item2 is CodeExpression)
            {
                expression = new CodeArrayCreateExpression(
                    _objectTypeReference,
                    IncrementArraySize((CodeExpression) item2));
            }
            else
            {
                CodeExpressionCollection expressions = (CodeExpressionCollection) item2;
                IncrementArraySizes(expressions);
                CodeRectangularArrayCreateExpression rectangularArrayCreateExpr = new CodeRectangularArrayCreateExpression(_objectTypeReference);
                rectangularArrayCreateExpr.Lengths.AddRange(expressions);
                expression = rectangularArrayCreateExpr;
            }

            return new CodeAssignStatement(
                new CodeVariableReferenceExpression(ParseExtendedID(0)),
                expression);
        }

        private object ParseIfStmtIfThenEndIf()
        {
            // <IfStmt> ::= If <Expr> Then <NL> <BlockStmtList> <ElseStmtList> End If <NL>
            AssertReductionCount(9);
            AssertString(0, "If");
            AssertString(2, "Then");
            AssertNewLine(3);
            AssertString(6, "End");
            AssertString(7, "If");
            AssertNewLine(8);
            CodeConditionStatement conditionStmt = new CodeConditionStatement((CodeExpression) GetTokenValue(1));

            if (GetTokenValue(5) != null)
            {
                Debug.Assert(_elseLine.HasValue);
                SetElseLine(conditionStmt, _elseLine);
            }

            _elseLine = null;
            SetLastLine(conditionStmt, GetTokenLine(6) - 1);
            AppendStatements(conditionStmt.TrueStatements, 4);
            AppendStatements(conditionStmt.FalseStatements, 5);
            return conditionStmt;
        }

        private object ParseIfStmtIfThen()
        {
            // <IfStmt> ::= If <Expr> Then <InlineStmt> <ElseOpt> <EndIfOpt> <NL>
            AssertReductionCount(7);
            AssertString(0, "If");
            AssertString(2, "Then");
            AssertNewLine(5);
            AssertNewLine(6);
            object item4 = GetTokenValue(4);
            CodeConditionStatement conditionStmt = new CodeConditionStatement(
                (CodeExpression) GetTokenValue(1),
                (CodeStatement) GetTokenValue(3));

            if (item4 != null)
            {
                conditionStmt.FalseStatements.Add((CodeStatement) item4);
            }

            return conditionStmt;
        }

        private object ParseElseStmtListElseIfThen()
        {
            // <ElseStmtList> ::= ElseIf <Expr> Then <NL> <BlockStmtList> <ElseStmtList>
            AssertReductionCount(6);
            AssertString(0, "ElseIf");
            AssertString(2, "Then");
            AssertNewLine(3);
            CodeConditionStatement conditionStmt = new CodeConditionStatement((CodeExpression) GetTokenValue(1));

            if (GetTokenValue(5) != null)
            {
                Debug.Assert(_elseLine.HasValue);
                SetElseLine(conditionStmt, _elseLine);
            }

            _elseLine = GetTokenLine(0) - 1;
            AppendStatements(conditionStmt.TrueStatements, 4);
            AppendStatements(conditionStmt.FalseStatements, 5);
            return conditionStmt;
        }

        private object ParseElseStmtListElseIfThen2()
        {
            // <ElseStmtList> ::= ElseIf <Expr> Then <InlineStmt> <NL> <ElseStmtList>
            AssertReductionCount(6);
            AssertString(0, "ElseIf");
            AssertString(2, "Then");
            AssertNewLine(4);
            CodeConditionStatement conditionStmt = new CodeConditionStatement(
                (CodeExpression) GetTokenValue(1),
                (CodeStatement) GetTokenValue(3));

            if (GetTokenValue(5) != null)
            {
                Debug.Assert(_elseLine.HasValue);
                SetElseLine(conditionStmt, _elseLine);
            }

            _elseLine = GetTokenLine(0) - 1;
            AppendStatements(conditionStmt.FalseStatements, 5);
            return conditionStmt;
        }

        private object ParseElseStmtListElse()
        {
            // <ElseStmtList> ::= Else <InlineStmt> <NL>
            AssertReductionCount(3);
            AssertString(0, "Else");
            AssertNewLine(2);
            Debug.Assert(!_elseLine.HasValue);
            _elseLine = GetTokenLine(0) - 1;
            object item1 = GetTokenValue(1);
            AssertIs<CodeStatement>(item1);
            return item1;
        }

        private object ParseElseStmtListElse2()
        {
            // <ElseStmtList> ::= Else <NL> <BlockStmtList>
            AssertReductionCount(3);
            AssertString(0, "Else");
            AssertNewLine(1);
            Debug.Assert(!_elseLine.HasValue);
            _elseLine = GetTokenLine(0) - 1;
            object item2 = GetTokenValue(2);
            AssertNullOrIs<CodeStatement, CodeStatementCollection>(item2);
            return item2;
        }

        private object ParseElseOptElse()
        {
            // <ElseOpt> ::= Else <InlineStmt>
            AssertReductionCount(2);
            AssertString(0, "Else");
            object item1 = GetTokenValue(1);
            AssertIs<CodeStatement>(item1);
            return item1;
        }

        private object ParseEndIfOptEndIf()
        {
            // <EndIfOpt> ::= End If
            AssertReductionCount(2);
            AssertString(0, "End");
            AssertString(1, "If");
            return null;
        }

        private object ParseWithStmtWithEndWith()
        {
            // <WithStmt> ::= With <Expr> <NL> <BlockStmtList> End With <NL>
            AssertReductionCount(7);
            AssertString(0, "With");
            AssertNewLine(2);
            AssertString(4, "End");
            AssertString(5, "With");
            AssertNewLine(6);
            CodeWithStatement withStmt = new CodeWithStatement((CodeExpression) GetTokenValue(1));
            AppendStatements(withStmt.Statements, 3);
            return withStmt;
        }

        private object ParseLoopStmtDoLoop()
        {
            // <LoopStmt> ::= Do <LoopType> <Expr> <NL> <BlockStmtList> Loop <NL>
            AssertReductionCount(7);
            AssertString(0, "Do");
            AssertNewLine(3);
            AssertString(5, "Loop");
            AssertNewLine(6);
            CodeExpression expression = (CodeExpression) GetTokenValue(2);
            string type = GetTokenText(1);

            if (StringUtils.CaseInsensitiveEquals(type, "Until"))
            {
                expression = InvertExpression(expression);
            }
            else
            {
                Debug.Assert(StringUtils.CaseInsensitiveEquals(type, "While"));
            }

            CodeWhileStatement whileStmt = new CodeWhileStatement(expression);
            SetLastLine(whileStmt, GetTokenLine(5) - 1);
            AppendStatements(whileStmt.Statements, 4);
            return whileStmt;
        }

        private object ParseLoopStmtDoLoop2()
        {
            // <LoopStmt> ::= Do <NL> <BlockStmtList> Loop <LoopType> <Expr> <NL>
            AssertReductionCount(7);
            AssertString(0, "Do");
            AssertNewLine(1);
            AssertString(3, "Loop");
            AssertNewLine(6);
            CodeExpression expression = (CodeExpression) GetTokenValue(5);
            string type = GetTokenText(4);

            if (StringUtils.CaseInsensitiveEquals(type, "Until"))
            {
                expression = InvertExpression(expression);
            }
            else
            {
                Debug.Assert(StringUtils.CaseInsensitiveEquals(type, "While"));
            }

            CodeDoWhileStatement doWhileStmt = new CodeDoWhileStatement(expression);
            SetLastLine(doWhileStmt, GetTokenLine(3) - 1);
            AppendStatements(doWhileStmt.Statements, 2);
            return doWhileStmt;
        }

        private object ParseLoopStmtDoLoop3()
        {
            // <LoopStmt> ::= Do <NL> <BlockStmtList> Loop <NL>
            AssertReductionCount(5);
            AssertString(0, "Do");
            AssertNewLine(1);
            AssertString(3, "Loop");
            AssertNewLine(4);
            CodeWhileStatement whileStmt = new CodeWhileStatement(
                new CodePrimitiveExpression(true));
            SetLastLine(whileStmt, GetTokenLine(3) - 1);
            AppendStatements(whileStmt.Statements, 2);
            return whileStmt;
        }

        private object ParseLoopStmtWhileWEnd()
        {
            // <LoopStmt> ::= While <Expr> <NL> <BlockStmtList> WEnd <NL>
            AssertReductionCount(6);
            AssertString(0, "While");
            AssertNewLine(2);
            AssertString(4, "Wend");
            AssertNewLine(5);
            CodeWhileStatement whileStmt = new CodeWhileStatement(
                (CodeExpression) GetTokenValue(1));
            SetLastLine(whileStmt, GetTokenLine(4) - 1);
            AppendStatements(whileStmt.Statements, 3);
            return whileStmt;
        }

        private object ParseLoopTypeWhile()
        {
            // <LoopType> ::= While
            AssertReductionCount(1);
            AssertString(0, "While");
            return "While";
        }

        private object ParseLoopTypeUntil()
        {
            // <LoopType> ::= Until
            AssertReductionCount(1);
            AssertString(0, "Until");
            return "Until";
        }

        private object ParseForStmtForEqToNext()
        {
            // <ForStmt> ::= For <ExtendedID> = <Expr> To <Expr> <StepOpt> <NL> <BlockStmtList> Next <NL>
            AssertReductionCount(11);
            AssertString(0, "For");
            AssertString(2, "=");
            AssertString(4, "To");
            AssertNewLine(7);
            AssertString(9, "Next");
            AssertNewLine(10);
            object item1 = GetTokenValue(1);
            object item6 = GetTokenValue(6);
            CodeExpression stepExpr;

            if (item6 == null)
            {
                stepExpr = new CodePrimitiveExpression(1);
            }
            else
            {
                stepExpr = (CodeExpression) item6;
            }

            CodeExpression variableExpr = (CodeVariableReferenceExpression) item1;
            CodeIterationStatement iterationStmt = new CodeIterationStatement(
                new CodeAssignStatement(
                    variableExpr,
                    (CodeExpression) GetTokenValue(3)),
                new CodeBinaryOperatorExpression(
                    variableExpr,
                    CodeBinaryOperatorType.LessThanOrEqual,
                    (CodeExpression) GetTokenValue(5)),
                new CodeAssignStatement(
                    variableExpr,
                    new CodeBinaryOperatorExpression(
                        variableExpr,
                        CodeBinaryOperatorType.Add,
                        stepExpr)));
            SetLastLine(iterationStmt, GetTokenLine(9) - 1);
            AppendStatements(iterationStmt.Statements, 8);
            return iterationStmt;
        }

        private object ParseForStmtForEachInNext()
        {
            // <ForStmt> ::= For Each <ExtendedID> In <Expr> <NL> <BlockStmtList> Next <NL>
            AssertReductionCount(9);
            AssertString(0, "For");
            AssertString(1, "Each");
            AssertString(3, "In");
            AssertNewLine(5);
            AssertString(7, "Next");
            AssertNewLine(8);
            object item6 = GetTokenValue(6);

            CodeForEachStatement forEachStmt = new CodeForEachStatement(
                new CodeTypeReference(typeof (object)),
                ((CodeVariableReferenceExpression) GetTokenValue(2)).VariableName,
                (CodeExpression) GetTokenValue(4));
            SetLastLine(forEachStmt, GetTokenLine(7) - 1);

            if (item6 != null)
            {
                AppendStatements(forEachStmt.Statements, 6);
            }

            return forEachStmt;
        }

        private object ParseStepOptStep()
        {
            // <StepOpt> ::= Step <Expr>
            AssertReductionCount(2);
            AssertString(0, "Step");
            object item1 = GetTokenValue(1);
            AssertIs<CodeExpression>(item1);
            return item1;
        }

        private object ParseSelectStmtSelectCaseEndSelect()
        {
            // <SelectStmt> ::= Select Case <Expr> <NL> <CaseStmtList> End Select <NL>
            AssertReductionCount(8);
            AssertString(0, "Select");
            AssertString(1, "Case");
            AssertNewLine(3);
            AssertString(5, "End");
            AssertString(6, "Select");
            AssertNewLine(7);

            CodeSwitchStatement switchStmt = new CodeSwitchStatement();
            switchStmt.Target = (CodeExpression) GetTokenValue(2);
            AppendSwitchOptions(switchStmt.Options, 4);
            int count = switchStmt.Options.Count;

            if (count > 0)
            {
                CodeSwitchOption lastOption = switchStmt.Options[count - 1];

                if (lastOption.Values.Count == 0)
                {
                    switchStmt.DefaultStatements.AddRange(lastOption.Statements);
                    switchStmt.Options.RemoveAt(count - 1);
                }
            }

            _elseLine = null;
            SetLastLine(switchStmt, GetTokenLine(5) - 1);
            return switchStmt;
        }

        private object ParseCaseStmtListCase()
        {
            // <CaseStmtList> ::= Case <ExprList> <NLOpt> <BlockStmtList> <CaseStmtList>
            AssertReductionCount(5);
            AssertString(0, "Case");
            AssertNewLine(2);
            object item4 = GetTokenValue(4);

            CodeSwitchOption switchOption = new CodeSwitchOption();
            AppendExpressions(switchOption.Values, 1);
            AppendStatements(switchOption.Statements, 3);

            if (item4 == null)
            {
                item4 = switchOption;
            }
            else
            {
                Debug.Assert(_elseLine != null);
                SetLastLine(switchOption, _elseLine);

                if (item4 is CodeSwitchOption)
                {
                    item4 = new CodeSwitchOptionCollection(
                        new CodeSwitchOption[]
                            {
                                switchOption,
                                (CodeSwitchOption) item4
                            });
                }
                else
                {
                    CodeSwitchOptionCollection collection = (CodeSwitchOptionCollection) item4;
                    collection.Insert(0, switchOption);
                    item4 = collection;
                }
            }

            _elseLine = GetTokenLine(0) - 1;
            return item4;
        }

        private object ParseCaseStmtListCaseElse()
        {
            // <CaseStmtList> ::= Case Else <NLOpt> <BlockStmtList>
            AssertReductionCount(4);
            AssertString(0, "Case");
            AssertString(1, "Else");
            AssertNewLine(2);
            Debug.Assert(!_elseLine.HasValue);
            _elseLine = GetTokenLine(0) - 1;

            CodeSwitchOption switchOption = new CodeSwitchOption();
            AppendStatements(switchOption.Statements, 3);
            return switchOption;
        }

        private object ParseExprListComma()
        {
            // <ExprList> ::= <Expr> , <ExprList>
            AssertReductionCount(3);
            AssertString(1, ",");
            return MergeExpressions(0, 2);
        }

        private object ParseSubSafeImpExprImp()
        {
            // <SubSafeImpExpr> ::= <SubSafeImpExpr> Imp <EqvExpr>
            return ParseImpExprImp();
        }

        private object ParseSubSafeEqvExprEqv()
        {
            // <SubSafeEqvExpr> ::= <SubSafeEqvExpr> Eqv <XorExpr>
            return ParseEqvExprEqv();
        }

        private object ParseSubSafeXorExprXor()
        {
            // <SubSafeXorExpr> ::= <SubSafeXorExpr> Xor <OrExpr>
            return ParseXorExprXor();
        }

        private object ParseSubSafeOrExprOr()
        {
            // <SubSafeOrExpr> ::= <SubSafeOrExpr> Or <AndExpr>
            return ParseOrExprOr();
        }

        private object ParseSubSafeAndExprAnd()
        {
            // <SubSafeAndExpr> ::= <SubSafeAndExpr> And <NotExpr>
            return ParseAndExprAnd();
        }

        private object ParseSubSafeNotExprNot()
        {
            // <SubSafeNotExpr> ::= Not <NotExpr>
            return ParseNotExprNot();
        }

        private object ParseSubSafeCompareExprIs()
        {
            // <SubSafeCompareExpr> ::= <SubSafeCompareExpr> Is <ConcatExpr>
            return ParseCompareExprIs();
        }

        private object ParseSubSafeCompareExprIsNot()
        {
            // <SubSafeCompareExpr> ::= <SubSafeCompareExpr> Is Not <ConcatExpr>
            return ParseCompareExprIsNot();
        }

        private object ParseSubSafeCompareExprGtEq()
        {
            // <SubSafeCompareExpr> ::= <SubSafeCompareExpr> >= <ConcatExpr>
            return ParseCompareExprGtEq();
        }

        private object ParseSubSafeCompareExprEqGt()
        {
            // <SubSafeCompareExpr> ::= <SubSafeCompareExpr> => <ConcatExpr>
            return ParseCompareExprEqGt();
        }

        private object ParseSubSafeCompareExprLtEq()
        {
            // <SubSafeCompareExpr> ::= <SubSafeCompareExpr> <= <ConcatExpr>
            return ParseCompareExprLtEq();
        }

        private object ParseSubSafeCompareExprEqLt()
        {
            // <SubSafeCompareExpr> ::= <SubSafeCompareExpr> =< <ConcatExpr>
            return ParseCompareExprEqLt();
        }

        private object ParseSubSafeCompareExprGt()
        {
            // <SubSafeCompareExpr> ::= <SubSafeCompareExpr> > <ConcatExpr>
            return ParseCompareExprGt();
        }

        private object ParseSubSafeCompareExprLt()
        {
            // <SubSafeCompareExpr> ::= <SubSafeCompareExpr> < <ConcatExpr>
            return ParseCompareExprLt();
        }

        private object ParseSubSafeCompareExprLtGt()
        {
            // <SubSafeCompareExpr> ::= <SubSafeCompareExpr> <> <ConcatExpr>
            return ParseCompareExprLtGt();
        }

        private object ParseSubSafeCompareExprEq()
        {
            // <SubSafeCompareExpr> ::= <SubSafeCompareExpr> = <ConcatExpr>
            return ParseCompareExprEq();
        }

        private object ParseSubSafeConcatExprAmp()
        {
            // <SubSafeConcatExpr> ::= <SubSafeConcatExpr> & <AddExpr>
            return ParseConcatExprAmp();
        }

        private object ParseSubSafeAddExprPlus()
        {
            // <SubSafeAddExpr> ::= <SubSafeAddExpr> + <ModExpr>
            return ParseAddExprPlus();
        }

        private object ParseSubSafeAddExprMinus()
        {
            // <SubSafeAddExpr> ::= <SubSafeAddExpr> - <ModExpr>
            return ParseAddExprMinus();
        }

        private object ParseSubSafeModExprMod()
        {
            // <SubSafeModExpr> ::= <SubSafeModExpr> Mod <IntDivExpr>
            return ParseModExprMod();
        }

        private object ParseSubSafeIntDivExprBackslash()
        {
            // <SubSafeIntDivExpr> ::= <SubSafeIntDivExpr> \ <MultExpr>
            return ParseIntDivExprBackslash();
        }

        private object ParseSubSafeMultExprTimes()
        {
            // <SubSafeMultExpr> ::= <SubSafeMultExpr> * <UnaryExpr>
            return ParseMultExprTimes();
        }

        private object ParseSubSafeMultExprDiv()
        {
            // <SubSafeMultExpr> ::= <SubSafeMultExpr> / <UnaryExpr>
            return ParseMultExprDiv();
        }

        private object ParseSubSafeUnaryExprMinus()
        {
            // <SubSafeUnaryExpr> ::= - <UnaryExpr>
            AssertReductionCount(2);
            return ParseUnaryExprMinus();
        }

        private object ParseSubSafeUnaryExprPlus()
        {
            // <SubSafeUnaryExpr> ::= + <UnaryExpr>
            return ParseUnaryExprPlus();
        }

        private object ParseSubSafeExpExprCaret()
        {
            // <SubSafeExpExpr> ::= <SubSafeValue> ^ <ExpExpr>
            return ParseExpExprCaret();
        }

        private object ParseSubSafeValueNew()
        {
            // <SubSafeValue> ::= New <LeftExpr>
            AssertReductionCount(2);
            AssertString(0, "New");
            return new CodeObjectCreateExpression(ParseExtendedID(1));
        }

        private object ParseImpExprImp()
        {
            // <ImpExpr> ::= <ImpExpr> Imp <EqvExpr>
            AssertReductionCount(3);
            AssertString(1, "Imp");
            return new CodeBinaryOperatorExpression(
                InvertExpression((CodeExpression) GetTokenValue(0)),
                CodeBinaryOperatorType.BitwiseOr,
                (CodeExpression) GetTokenValue(2));
        }

        private object ParseEqvExprEqv()
        {
            // <EqvExpr> ::= <EqvExpr> Eqv <XorExpr>
            AssertReductionCount(3);
            AssertString(1, "Eqv");
            return InvertExpression(
                new CodeMethodInvokeExpression(
                    new CodeTypeReferenceExpression(new CodeTypeReference(typeof (Operators))),
                    "EqvObject",
                    (CodeExpression) GetTokenValue(0),
                    (CodeExpression) GetTokenValue(2)));
        }

        private object ParseXorExprXor()
        {
            // <XorExpr> ::= <XorExpr> Xor <OrExpr>
            AssertReductionCount(3);
            AssertString(1, "Xor");
            return new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression(new CodeTypeReference(typeof (Operators))),
                "XorObject",
                (CodeExpression) GetTokenValue(0),
                (CodeExpression) GetTokenValue(2));
        }

        private object ParseOrExprOr()
        {
            // <OrExpr> ::= <OrExpr> Or <AndExpr>
            //TODO: think about bitwise versus boolean
            AssertReductionCount(3);
            AssertString(1, "Or");
            return new CodeBinaryOperatorExpression(
                (CodeExpression) GetTokenValue(0),
                CodeBinaryOperatorType.BitwiseOr,
                (CodeExpression) GetTokenValue(2));
        }

        private object ParseAndExprAnd()
        {
            // <AndExpr> ::= <AndExpr> And <NotExpr>
            //TODO: think about bitwise versus boolean
            AssertReductionCount(3);
            AssertString(1, "And");
            return new CodeBinaryOperatorExpression(
                (CodeExpression) GetTokenValue(0),
                CodeBinaryOperatorType.BitwiseAnd,
                (CodeExpression) GetTokenValue(2));
        }

        private object ParseNotExprNot()
        {
            // <NotExpr> ::= Not <NotExpr>
            AssertReductionCount(2);
            AssertString(0, "Not");
            return InvertExpression((CodeExpression) GetTokenValue(1));
        }

        private object ParseCompareExprIs()
        {
            // <CompareExpr> ::= <CompareExpr> Is <ConcatExpr>
            AssertReductionCount(3);
            AssertString(1, "Is");
            return new CodeBinaryOperatorExpression(
                (CodeExpression) GetTokenValue(0),
                CodeBinaryOperatorType.IdentityEquality,
                (CodeExpression) GetTokenValue(2));
        }

        private object ParseCompareExprIsNot()
        {
            // <CompareExpr> ::= <CompareExpr> Is Not <ConcatExpr>
            AssertReductionCount(4);
            AssertString(1, "Is");
            AssertString(2, "Not");
            return InvertExpression(
                new CodeBinaryOperatorExpression(
                    (CodeExpression) GetTokenValue(0),
                    CodeBinaryOperatorType.IdentityEquality,
                    (CodeExpression) GetTokenValue(3)));
        }

        private object ParseCompareExprGtEq()
        {
            // <CompareExpr> ::= <CompareExpr> >= <ConcatExpr>
            AssertReductionCount(3);
            AssertString(1, ">=");
            return new CodeBinaryOperatorExpression(
                (CodeExpression) GetTokenValue(0),
                CodeBinaryOperatorType.GreaterThanOrEqual,
                (CodeExpression) GetTokenValue(2));
        }

        private object ParseCompareExprEqGt()
        {
            // <CompareExpr> ::= <CompareExpr> => <ConcatExpr>
            AssertReductionCount(3);
            AssertString(1, "=>");
            return new CodeBinaryOperatorExpression(
                (CodeExpression) GetTokenValue(0),
                CodeBinaryOperatorType.GreaterThanOrEqual,
                (CodeExpression) GetTokenValue(2));
        }

        private object ParseCompareExprLtEq()
        {
            // <CompareExpr> ::= <CompareExpr> <= <ConcatExpr>
            AssertReductionCount(3);
            AssertString(1, "<=");
            return new CodeBinaryOperatorExpression(
                (CodeExpression) GetTokenValue(0),
                CodeBinaryOperatorType.LessThanOrEqual,
                (CodeExpression) GetTokenValue(2));
        }

        private object ParseCompareExprEqLt()
        {
            // <CompareExpr> ::= <CompareExpr> =< <ConcatExpr>
            AssertReductionCount(3);
            AssertString(1, "=<");
            return new CodeBinaryOperatorExpression(
                (CodeExpression) GetTokenValue(0),
                CodeBinaryOperatorType.LessThanOrEqual,
                (CodeExpression) GetTokenValue(2));
        }

        private object ParseCompareExprGt()
        {
            // <CompareExpr> ::= <CompareExpr> > <ConcatExpr>
            AssertReductionCount(3);
            AssertString(1, ">");
            return new CodeBinaryOperatorExpression(
                (CodeExpression) GetTokenValue(0),
                CodeBinaryOperatorType.GreaterThan,
                (CodeExpression) GetTokenValue(2));
        }

        private object ParseCompareExprLt()
        {
            // <CompareExpr> ::= <CompareExpr> < <ConcatExpr>
            AssertReductionCount(3);
            AssertString(1, "<");
            return new CodeBinaryOperatorExpression(
                (CodeExpression) GetTokenValue(0),
                CodeBinaryOperatorType.LessThan,
                (CodeExpression) GetTokenValue(2));
        }

        private object ParseCompareExprLtGt()
        {
            // <CompareExpr> ::= <CompareExpr> <> <ConcatExpr>
            AssertReductionCount(3);
            AssertString(1, "<>");
            return new CodeBinaryOperatorExpression(
                (CodeExpression) GetTokenValue(0),
                CodeBinaryOperatorType.IdentityInequality,
                (CodeExpression) GetTokenValue(2));
        }

        private object ParseCompareExprEq()
        {
            // <CompareExpr> ::= <CompareExpr> = <ConcatExpr>
            AssertReductionCount(3);
            AssertString(1, "=");
            return new CodeBinaryOperatorExpression(
                (CodeExpression) GetTokenValue(0),
                CodeBinaryOperatorType.ValueEquality,
                (CodeExpression) GetTokenValue(2));
        }

        private object ParseConcatExprAmp()
        {
            // <ConcatExpr> ::= <ConcatExpr> & <AddExpr>
            AssertReductionCount(3);
            AssertString(1, "&");

            object item0 = GetTokenValue(0);
            object item2 = GetTokenValue(2);
            CodeMethodInvokeExpression invokeExpr = null;
            bool handled = false;

            if (item0 is CodeMethodInvokeExpression)
            {
                invokeExpr = (CodeMethodInvokeExpression) item0;
                CodeMethodReferenceExpression methodRefExpr = invokeExpr.Method;

                if (methodRefExpr.MethodName == "Concat" && methodRefExpr.TargetObject is CodeTypeReferenceExpression)
                {
                    CodeTypeReferenceExpression typeExpr = (CodeTypeReferenceExpression) methodRefExpr.TargetObject;
                    CodeTypeReference typeRef = typeExpr.Type;

                    if (typeRef.BaseType == typeof (string).FullName &&
                        typeRef.ArrayRank == 0 &&
                        typeRef.ArrayElementType == null &&
                        typeRef.TypeArguments.Count == 0)
                    {
                        invokeExpr.Parameters.Add((CodeExpression) item2);
                        handled = true;
                    }
                }
            }

            if (!handled)
            {
                invokeExpr = new CodeMethodInvokeExpression(
                    new CodeTypeReferenceExpression(
                        new CodeTypeReference(typeof (string))),
                    "Concat",
                    (CodeExpression) GetTokenValue(0),
                    (CodeExpression) GetTokenValue(2));
            }

            return invokeExpr;
        }

        private object ParseAddExprPlus()
        {
            // <AddExpr> ::= <AddExpr> + <ModExpr>
            AssertReductionCount(3);
            AssertString(1, "+");
            return new CodeBinaryOperatorExpression(
                (CodeExpression) GetTokenValue(0),
                CodeBinaryOperatorType.Add,
                (CodeExpression) GetTokenValue(2));
        }

        private object ParseAddExprMinus()
        {
            // <AddExpr> ::= <AddExpr> - <ModExpr>
            AssertReductionCount(3);
            AssertString(1, "-");
            return new CodeBinaryOperatorExpression(
                (CodeExpression) GetTokenValue(0),
                CodeBinaryOperatorType.Subtract,
                (CodeExpression) GetTokenValue(2));
        }

        private object ParseModExprMod()
        {
            // <ModExpr> ::= <ModExpr> Mod <IntDivExpr>
            AssertReductionCount(3);
            AssertString(1, "Mod");
            return new CodeBinaryOperatorExpression(
                (CodeExpression) GetTokenValue(0),
                CodeBinaryOperatorType.Modulus,
                (CodeExpression) GetTokenValue(2));
        }

        private object ParseIntDivExprBackslash()
        {
            // <IntDivExpr> ::= <IntDivExpr> \ <MultExpr>
            AssertReductionCount(3);
            AssertString(1, @"\");
            return new CodeBinaryOperatorExpression(
                new CodeCastExpression(
                    new CodeTypeReference(typeof (long)),
                    new CodeMethodInvokeExpression(
                        new CodeTypeReferenceExpression(new CodeTypeReference(typeof (Math))),
                        "Round",
                        (CodeExpression) GetTokenValue(0))),
                CodeBinaryOperatorType.Divide,
                new CodeCastExpression(
                    new CodeTypeReference(typeof (long)),
                    new CodeMethodInvokeExpression(
                        new CodeTypeReferenceExpression(new CodeTypeReference(typeof (Math))),
                        "Round",
                        (CodeExpression) GetTokenValue(2))));
        }

        private object ParseMultExprTimes()
        {
            // <MultExpr> ::= <MultExpr> * <UnaryExpr>
            AssertReductionCount(3);
            AssertString(1, "*");
            return new CodeBinaryOperatorExpression(
                (CodeExpression) GetTokenValue(0),
                CodeBinaryOperatorType.Multiply,
                (CodeExpression) GetTokenValue(2));
        }

        private object ParseMultExprDiv()
        {
            // <MultExpr> ::= <MultExpr> / <UnaryExpr>
            AssertReductionCount(3);
            AssertString(1, "/");
            return new CodeBinaryOperatorExpression(
                (CodeExpression) GetTokenValue(0),
                CodeBinaryOperatorType.Divide,
                (CodeExpression) GetTokenValue(2));
        }

        private object ParseUnaryExprMinus()
        {
            // <UnaryExpr> ::= - <UnaryExpr>
            AssertReductionCount(2);
            AssertString(0, "-");
            object item1 = GetTokenValue(1);

            if (item1 is CodePrimitiveExpression)
            {
                CodePrimitiveExpression primitiveExpr = (CodePrimitiveExpression) item1;

                if (primitiveExpr.Value is int)
                {
                    primitiveExpr.Value = -(int) primitiveExpr.Value;
                    return primitiveExpr;
                }
                else if (primitiveExpr.Value is double)
                {
                    primitiveExpr.Value = -(double) primitiveExpr.Value;
                    return primitiveExpr;
                }
                else if (primitiveExpr.Value is bool)
                {
                    primitiveExpr.Value = ((bool) primitiveExpr.Value ? 1 : 0);
                    return primitiveExpr;
                }
                else if (primitiveExpr.Value == null)
                {
                    primitiveExpr.Value = 0;
                    return primitiveExpr;
                }
            }
            else if (item1 is CodeObjectCreateExpression)
            {
                CodeObjectCreateExpression objectExpr = (CodeObjectCreateExpression) item1;

                if (objectExpr.Parameters.Count == 3)
                {
                    DateTime beforeDate = new DateTime(
                        (int) ((CodePrimitiveExpression) objectExpr.Parameters[0]).Value,
                        (int) ((CodePrimitiveExpression) objectExpr.Parameters[1]).Value,
                        (int) ((CodePrimitiveExpression) objectExpr.Parameters[2]).Value);
                    DateTime afterDate = new DateTime(2*new DateTime(1899, 12, 30).Ticks - beforeDate.Ticks);
                    ((CodePrimitiveExpression) objectExpr.Parameters[0]).Value = afterDate.Year;
                    ((CodePrimitiveExpression) objectExpr.Parameters[1]).Value = afterDate.Month;
                    ((CodePrimitiveExpression) objectExpr.Parameters[2]).Value = afterDate.Day;
                }
                else
                {
                    DateTime beforeDate = new DateTime(
                        (int) ((CodePrimitiveExpression) objectExpr.Parameters[0]).Value,
                        (int) ((CodePrimitiveExpression) objectExpr.Parameters[1]).Value,
                        (int) ((CodePrimitiveExpression) objectExpr.Parameters[2]).Value,
                        (int) ((CodePrimitiveExpression) objectExpr.Parameters[3]).Value,
                        (int) ((CodePrimitiveExpression) objectExpr.Parameters[4]).Value,
                        (int) ((CodePrimitiveExpression) objectExpr.Parameters[5]).Value);
                    DateTime afterDate = new DateTime(2*new DateTime(1899, 12, 30).Ticks - beforeDate.Ticks);
                    ((CodePrimitiveExpression) objectExpr.Parameters[0]).Value = afterDate.Year;
                    ((CodePrimitiveExpression) objectExpr.Parameters[1]).Value = afterDate.Month;
                    ((CodePrimitiveExpression) objectExpr.Parameters[2]).Value = afterDate.Day;
                    ((CodePrimitiveExpression) objectExpr.Parameters[3]).Value = afterDate.Hour;
                    ((CodePrimitiveExpression) objectExpr.Parameters[4]).Value = afterDate.Minute;
                    ((CodePrimitiveExpression) objectExpr.Parameters[5]).Value = afterDate.Second;
                }

                return objectExpr;
            }

            return new CodeUnaryMinusExpression((CodeExpression) GetTokenValue(1));
        }

        private object ParseUnaryExprPlus()
        {
            // <UnaryExpr> ::= + <UnaryExpr>
            //this is probably always redundant or an error (if expr doesn't make sense, like a string)
            AssertReductionCount(2);
            AssertString(0, "+");
            object item1 = GetTokenValue(1);
            AssertIs<CodeExpression>(item1);
            return item1;
        }

        private object ParseExpExprCaret()
        {
            // <ExpExpr> ::= <Value> ^ <ExpExpr>
            AssertReductionCount(3);
            AssertString(1, "^");
            return new CodeMethodInvokeExpression(
                new CodeTypeReferenceExpression(new CodeTypeReference(typeof (Math))),
                "Pow",
                (CodeExpression) GetTokenValue(0),
                (CodeExpression) GetTokenValue(2));
        }

        private object ParseValueLParanRParan()
        {
            // <Value> ::= ( <Expr> )
            AssertReductionCount(3);
            AssertString(0, "(");
            AssertString(2, ")");
            object item1 = GetTokenValue(1);
            AssertIs<CodeExpression>(item1);
            return item1;
        }

        private object ParseValueNew()
        {
            // <Value> ::= New <LeftExpr>
            AssertReductionCount(2);
            AssertString(0, "New");
            return new CodeObjectCreateExpression(ParseExtendedID(1));
        }

        private object ParseConstExprFloatLiteral()
        {
            // <ConstExpr> ::= FloatLiteral
            AssertReductionCount(1);
            return new CodePrimitiveExpression(double.Parse(GetTokenText(0)));
        }

        private object ParseConstExprStringLiteral()
        {
            // <ConstExpr> ::= StringLiteral
            AssertReductionCount(1);
            string str = GetTokenText(0);
            return new CodePrimitiveExpression(str.Substring(1, str.Length - 2).Replace("\"\"", "\""));
        }

        private object ParseConstExprDateLiteral()
        {
            // <ConstExpr> ::= DateLiteral
            AssertReductionCount(1);
            string str = GetTokenText(0);
            DateTime date = DateTime.Parse(str.Substring(1, str.Length - 2));
            CodeObjectCreateExpression objectExpr = new CodeObjectCreateExpression(
                new CodeTypeReference(typeof (DateTime)),
                new CodePrimitiveExpression(date.Year),
                new CodePrimitiveExpression(date.Month),
                new CodePrimitiveExpression(date.Day));

            if (date.TimeOfDay != TimeSpan.Zero)
            {
                objectExpr.Parameters.Add(new CodePrimitiveExpression(date.Hour));
                objectExpr.Parameters.Add(new CodePrimitiveExpression(date.Minute));
                objectExpr.Parameters.Add(new CodePrimitiveExpression(date.Second));
            }

            return objectExpr;
        }

        private object ParseBoolLiteralTrue()
        {
            // <BoolLiteral> ::= True
            AssertReductionCount(1);
            AssertString(0, "True");
            return new CodePrimitiveExpression(true);
        }

        private object ParseBoolLiteralFalse()
        {
            // <BoolLiteral> ::= False
            AssertReductionCount(1);
            AssertString(0, "False");
            return new CodePrimitiveExpression(false);
        }

        private object ParseIntLiteralIntLiteral()
        {
            // <IntLiteral> ::= IntLiteral
            AssertReductionCount(1);
            string str = GetTokenText(0);
            object value;
            int result;

            if (int.TryParse(str, out result))
            {
                value = result;
            }
            else
            {
                value = double.Parse(str);
            }

            return new CodePrimitiveExpression(value);
        }

        private object ParseIntLiteralHexLiteral()
        {
            // <IntLiteral> ::= HexLiteral
            AssertReductionCount(1);
            string str = GetTokenText(0).Substring(2);

            if (str.EndsWith("&"))
            {
                str = str.Substring(0, str.Length - 1);
            }

            return new CodePrimitiveExpression(int.Parse(str, NumberStyles.HexNumber));
        }

        private object ParseIntLiteralOctLiteral()
        {
            // <IntLiteral> ::= OctLiteral
            AssertReductionCount(1);
            string str = GetTokenText(0).Substring(1);

            if (str.EndsWith("&"))
            {
                str = str.Substring(0, str.Length - 1);
            }

            return new CodePrimitiveExpression(Convert.ToInt32(str, 8));
        }

        private object ParseNothingNothing()
        {
            // <Nothing> ::= Nothing
            AssertReductionCount(1);
            AssertString(0, "Nothing");
            return new CodePrimitiveExpression(null);
        }

        private object ParseNothingNull()
        {
            // <Nothing> ::= Null
            AssertReductionCount(1);
            AssertString(0, "Null");
            return new CodeFieldReferenceExpression(
                new CodeTypeReferenceExpression(new CodeTypeReference(typeof (DBNull))), "Value");
        }

        private object ParseNothingEmpty()
        {
            // <Nothing> ::= Empty
            AssertReductionCount(1);
            AssertString(0, "Empty");
            return new CodePrimitiveExpression(null);
        }

        //=======================================

        private object ParseID()
        {
            AssertReductionCount(1);
            return new CodeVariableReferenceExpression(GetTokenText(0));
        }

        private string ParseExtendedID(int index)
        {
            return ((CodeVariableReferenceExpression) GetTokenValue(index)).VariableName;
        }

        //=======================================

        private int? GetTokenLine(int index)
        {
            return ((Token) _parser.GetReductionSyntaxNode(index)).Line;
        }

        private object GetTokenValue(int index)
        {
            return ((Token) _parser.GetReductionSyntaxNode(index)).Value;
        }

        private string GetTokenText(int index)
        {
            return (string) GetTokenValue(index);
        }

        //=======================================

        private void AssertReductionCount(int count)
        {
            Debug.Assert(_parser.ReductionCount == count);
        }

        private void AssertNewLine(int index)
        {
            object value = GetTokenValue(index);
            Debug.Assert(value == null || value == _newLinePlaceholder);
        }

        private void AssertString(int index, params string[] values)
        {
            string text = (string) GetTokenValue(index);

            foreach (string value in values)
            {
                if (StringUtils.CaseInsensitiveEquals(text, value))
                {
                    return;
                }
            }

            Debug.Assert(false);
        }

        private static void AssertIs<T>(object item)
        {
            Debug.Assert(item is T);
        }

        private static void AssertIs<T1, T2>(object item)
        {
            Debug.Assert(item is T1 || item is T2);
        }

        private static void AssertNullOrIs<T>(object item)
        {
            Debug.Assert(item == null || item is T);
        }

        private static void AssertNullOrIs<T1, T2>(object item)
        {
            Debug.Assert(item == null || item is T1 || item is T2);
        }

        //=======================================

        private CodeMemberMethod BuildMemberMethod(object attributes, object name, object parameters, object statements, int? lastLine)
        {
            CodeMemberMethod memberMethod = new CodeMemberMethod();
            SetLastLine(memberMethod, lastLine - 1);

            if (_defaultAttribute != null)
            {
                //TODO: warning - default is not applicable to methods
                _defaultAttribute = null;
            }

            if (attributes == null)
            {
                memberMethod.Attributes = 0;
            }
            else
            {
                CodeDomUtils.SetMemberAttributes(memberMethod, (MemberAttributes) attributes);
            }

            memberMethod.Name = ((CodeVariableReferenceExpression) name).VariableName;
            AppendParameterDeclarationExpressions(memberMethod.Parameters, parameters);
            AppendStatements(memberMethod.Statements, statements);
            return memberMethod;
        }

        private CodeMemberMethod BuildMemberMethodFunction(object attributes, object name, object parameters, object statements, int? lastLine)
        {
            CodeMemberMethod memberMethod = BuildMemberMethod(
                attributes,
                name,
                parameters,
                statements,
                lastLine);
            memberMethod.ReturnType = _objectTypeReference;
            return memberMethod;
        }

        private static object BuildMemberFields(object thisField, object otherFields, MemberAttributes attributes)
        {
            CodeMemberField memberField;

            if (thisField is CodeVariableReferenceExpression)
            {
                memberField = new CodeMemberField(_objectTypeReference, ((CodeVariableReferenceExpression) thisField).VariableName);
            }
            else
            {
                memberField = (CodeMemberField) thisField;
            }

            CodeDomUtils.SetMemberAttributes(memberField, attributes);

            if (otherFields == null)
            {
                return memberField;
            }
            else
            {
                List<CodeObject> members = new List<CodeObject>();
                members.Add(memberField);

                if (otherFields is CodeVariableDeclarationStatement)
                {
                    CodeVariableDeclarationStatement variableStmt = (CodeVariableDeclarationStatement) otherFields;
                    memberField = new CodeMemberField(variableStmt.Type, variableStmt.Name);
                    CodeDomUtils.SetMemberAttributes(memberField, attributes);
                    members.Add(memberField);
                }
                else
                {
                    foreach (CodeVariableDeclarationStatement variableStmt in (IEnumerable) otherFields)
                    {
                        memberField = new CodeMemberField(variableStmt.Type, variableStmt.Name);
                        CodeDomUtils.SetMemberAttributes(memberField, attributes);
                        members.Add(memberField);
                    }
                }

                return members;
            }
        }

        //=======================================

        private void IncorporateComments(CodeTypeMember typeMember)
        {
            int? line = GetLine(typeMember);
            int extraCount = _comments.Count;

            for (int i = 0; i < extraCount && _comments.Keys[0] < line; i++)
            {
                CodeStatement statement = _comments.Values[0];

                if (statement is CodeCommentStatement)
                {
                    typeMember.Comments.Add((CodeCommentStatement) statement);
                }

                _comments.RemoveAt(0);
            }

            if (typeMember is CodeTypeDeclaration)
            {
                CodeTypeDeclaration typeDecl = (CodeTypeDeclaration) typeMember;

                foreach (CodeTypeMember subTypeMember in typeDecl.Members)
                {
                    IncorporateComments(subTypeMember);
                }

                CodeSnippetTypeMember snippetTypeMember = new CodeSnippetTypeMember(string.Empty);
                extraCount = _comments.Count;
                int? lastLine = GetLastLine(typeDecl);

                for (int i = 0; i < extraCount && _comments.Keys[0] < lastLine; i++)
                {
                    snippetTypeMember.Comments.Add(_comments.Values[0]);
                    _comments.RemoveAt(0);
                }

                if (snippetTypeMember.Comments.Count > 0)
                {
                    typeDecl.Members.Add(snippetTypeMember);
                }
            }
            else if (typeMember is CodeMemberMethod)
            {
                CodeMemberMethod memberMethod = (CodeMemberMethod) typeMember;
                IncorporateComments(memberMethod.Statements, GetLastLine(typeMember));
            }
            else if (typeMember is CodeMemberProperty)
            {
                CodeMemberProperty memberProperty = (CodeMemberProperty) typeMember;
                int? lastLine = GetLastLine(typeMember);
                IncorporateComments(memberProperty.GetStatements, lastLine);
                IncorporateComments(memberProperty.SetStatements, lastLine);
            }
            else if (typeMember == null)
            {
                Debug.Assert(false, "Unexpected null type member");
            }
            else
            {
                Debug.Assert(typeMember is CodeMemberField, "Unexpected type member type: " + typeMember.GetType().Name);
            }
        }

        private void IncorporateComments(CodeStatementCollection statements, int? lastLine)
        {
            for (int i = 0; i < statements.Count; i++)
            {
                CodeStatement statement = statements[i];
                int? firstLine = GetLine(statement);
                int extraCount = _comments.Count;

                for (int j = 0; j < extraCount && _comments.Keys[0] < firstLine; j++)
                {
                    statements.Insert(i + j, _comments.Values[0]);
                    _comments.RemoveAt(0);
                }

                IncorporateComments(statement);
            }

            {
                int extraCount = _comments.Count;

                for (int i = 0; i < extraCount && _comments.Keys[0] <= lastLine; i++)
                {
                    statements.Add(_comments.Values[0]);
                    _comments.RemoveAt(0);
                }
            }
        }

        private void IncorporateComments(CodeStatement statement)
        {
            if (statement is CodeSwitchStatement)
            {
                CodeSwitchStatement switchStmt = (CodeSwitchStatement) statement;

                foreach (CodeSwitchOption option in switchStmt.Options)
                {
                    IncorporateComments(option.Statements, GetLastLine(option));
                }

                IncorporateComments(switchStmt.DefaultStatements, GetLastLine(switchStmt));
            }
            else if (statement is CodeConditionStatement)
            {
                CodeConditionStatement conditionStmt = (CodeConditionStatement) statement;

                if (conditionStmt.FalseStatements.Count > 0)
                {
                    IncorporateComments(conditionStmt.TrueStatements, GetElseLine(conditionStmt));
                    IncorporateComments(conditionStmt.FalseStatements, GetLastLine(conditionStmt));
                }
                else
                {
                    IncorporateComments(conditionStmt.TrueStatements, GetLastLine(conditionStmt));
                }
            }
            else if (statement is CodeIterationStatement)
            {
                CodeIterationStatement iterationStmt = (CodeIterationStatement) statement;
                IncorporateComments(iterationStmt.Statements, GetLastLine(iterationStmt));
            }
            else if (statement == null)
            {
                Debug.Assert(false, "Unexpected null statement");
            }
            else
            {
                Debug.Assert(statement is CodeAssignStatement
                             || statement is CodeCommentStatement
                             || statement is CodeExpressionStatement
                             || statement is CodeMethodReturnStatement
                             || statement is CodeSnippetStatement
                             || statement is CodeVariableDeclarationStatement,
                             "Unexpected statement type: " + statement.GetType().Name);
            }
        }

        //=======================================

        private void AppendStatements(CodeStatementCollection collection, int index)
        {
            AppendStatements(collection, GetTokenValue(index));
        }

        private void AppendExpressions(CodeExpressionCollection collection, int index)
        {
            AppendExpressions(collection, GetTokenValue(index));
        }

        private void AppendParameterDeclarationExpressions(CodeParameterDeclarationExpressionCollection collection, int index)
        {
            AppendParameterDeclarationExpressions(collection, GetTokenValue(index));
        }

        private void AppendSwitchOptions(CodeSwitchOptionCollection collection, int index)
        {
            AppendSwitchOptions(collection, GetTokenValue(index));
        }

        private static void AppendStatements(CodeStatementCollection collection, object obj)
        {
            if (obj != null)
            {
                if (obj is CodeStatement)
                {
                    collection.Add((CodeStatement) obj);
                }
                else
                {
                    collection.AddRange((CodeStatementCollection) obj);
                }
            }
        }

        private static void AppendExpressions(CodeExpressionCollection collection, object obj)
        {
            if (obj != null)
            {
                if (obj is CodeExpression)
                {
                    collection.Add((CodeExpression) obj);
                }
                else
                {
                    collection.AddRange((CodeExpressionCollection) obj);
                }
            }
        }

        private static void AppendParameterDeclarationExpressions(CodeParameterDeclarationExpressionCollection collection, object obj)
        {
            if (obj != null)
            {
                if (obj is CodeParameterDeclarationExpression)
                {
                    collection.Add((CodeParameterDeclarationExpression) obj);
                }
                else
                {
                    collection.AddRange((CodeParameterDeclarationExpressionCollection) obj);
                }
            }
        }

        private static void AppendSwitchOptions(CodeSwitchOptionCollection collection, object obj)
        {
            if (obj != null)
            {
                if (obj is CodeSwitchOption)
                {
                    collection.Add((CodeSwitchOption) obj);
                }
                else
                {
                    collection.AddRange((CodeSwitchOptionCollection) obj);
                }
            }
        }

        //=======================================

        private object MergeObjects(int leftIndex, int rightIndex)
        {
            return Merge<List<CodeObject>, CodeObject>(leftIndex, rightIndex);
        }

        private object MergeStatements(int leftIndex, int rightIndex)
        {
            return Merge<CodeStatementCollection, CodeStatement>(leftIndex, rightIndex);
        }

        private object MergeExpressions(int leftIndex, int rightIndex)
        {
            return Merge<CodeExpressionCollection, CodeExpression>(leftIndex, rightIndex);
        }

        private object MergeParameterDeclarationExpressions(int leftIndex, int rightIndex)
        {
            return Merge<CodeParameterDeclarationExpressionCollection, CodeParameterDeclarationExpression>(leftIndex, rightIndex);
        }

        private object Merge<TCollection, TItem>(int leftIndex, int rightIndex)
            where TCollection : IList, new()
        {
            object leftObj = GetTokenValue(leftIndex);
            object rightObj = GetTokenValue(rightIndex);

            if (leftObj == null && rightObj == null)
            {
                return null;
            }
            else if (leftObj is TItem && rightObj == null)
            {
                return leftObj;
            }
            else if (leftObj == null && rightObj is TItem)
            {
                return rightObj;
            }
            else
            {
                TCollection items = new TCollection();

                if (leftObj is TItem)
                {
                    items.Add(leftObj);
                }
                else if (leftObj is CodeMemberField) //TODO: may require cleanup
                {
                    CodeMemberField memberField = (CodeMemberField) leftObj;
                    items.Add(new CodeVariableDeclarationStatement(_objectTypeReference, memberField.Name, memberField.InitExpression));
                }
                else if (leftObj != null)
                {
                    //workaround given Const can exist in methods

                    foreach (object item in (IEnumerable) leftObj)
                    {
                        if (item is CodeMemberField)
                        {
                            CodeMemberField memberField = (CodeMemberField) item;
                            items.Add(new CodeVariableDeclarationStatement(
                                          memberField.Type,
                                          memberField.Name,
                                          memberField.InitExpression));
                        }
                        else
                        {
                            items.Add(item);
                        }
                    }
                }

                if (rightObj is TItem)
                {
                    items.Add(rightObj);
                }
                else if (rightObj != null)
                {
                    foreach (object item in (IEnumerable) rightObj)
                    {
                        items.Add(item);
                    }
                }

                return items;
            }
        }

        //=======================================

        private static CodeExpression InvertExpression(CodeExpression expression)
        {
            return new CodeBinaryOperatorExpression(
                expression,
                CodeBinaryOperatorType.IdentityInequality,
                new CodePrimitiveExpression(true));
        }

        private static void SetRootTarget(CodeExpression expression, CodeExpression newTargetExpr)
        {
            CodeExpression childExpr = null;
            CodeExpression parentExpr = expression;

            while (parentExpr != null)
            {
                if (parentExpr is CodePropertyReferenceExpression)
                {
                    parentExpr = ((CodePropertyReferenceExpression) expression).TargetObject;
                }
                else if (parentExpr is CodeMethodInvokeExpression)
                {
                    parentExpr = ((CodeMethodInvokeExpression) expression).Method.TargetObject;
                }
                else
                {
                    if (childExpr is CodePropertyReferenceExpression)
                    {
                        ((CodePropertyReferenceExpression) childExpr).TargetObject = new CodePropertyReferenceExpression(
                            newTargetExpr,
                            ((CodeVariableReferenceExpression) expression).VariableName);
                    }
                    else
                    {
                        ((CodeMethodInvokeExpression) childExpr).Method.TargetObject = new CodePropertyReferenceExpression(
                            newTargetExpr,
                            ((CodeVariableReferenceExpression) expression).VariableName);
                    }

                    break;
                }

                if (parentExpr == null)
                {
                    if (expression is CodePropertyReferenceExpression)
                    {
                        ((CodePropertyReferenceExpression) expression).TargetObject = newTargetExpr;
                    }
                    else
                    {
                        ((CodeMethodInvokeExpression) expression).Method.TargetObject = newTargetExpr;
                    }

                    break;
                }
                else
                {
                    childExpr = expression;
                    expression = parentExpr;
                }
            }
        }

        //=======================================

        private static CodeExpression IncrementArraySize(CodeExpression before)
        {
            CodePrimitiveExpression primitiveExpr = before as CodePrimitiveExpression;

            if (primitiveExpr != null && primitiveExpr.Value is int)
            {
                primitiveExpr.Value = ((int) primitiveExpr.Value) + 1;
                return before;
            }
            else
            {
                return new CodeBinaryOperatorExpression(
                    before,
                    CodeBinaryOperatorType.Add,
                    new CodePrimitiveExpression(1));
            }
        }

        private static void IncrementArraySizes(CodeExpressionCollection expressions)
        {
            for (int i = 0; i < expressions.Count; i++)
            {
                expressions[i] = IncrementArraySize(expressions[i]);
            }
        }

        //=======================================

        private IDictionary<CodeObject, int?> _lines;
        private IDictionary<CodeObject, int?> _elseLines;
        private IDictionary<CodeObject, int?> _lastLines;

        private int? GetLine(CodeObject obj)
        {
            return GetValue(_lines, obj);
        }

        private void SetLine(CodeObject obj, int? line)
        {
            SetValue(ref _lines, obj, line);
        }

        private int? GetElseLine(CodeObject obj)
        {
            return GetValue(_elseLines, obj);
        }

        private void SetElseLine(CodeObject obj, int? elseLine)
        {
            SetValue(ref _elseLines, obj, elseLine);
        }

        private int? GetLastLine(CodeObject obj)
        {
            return GetValue(_lastLines, obj);
        }

        private void SetLastLine(CodeObject obj, int? lastLine)
        {
            SetValue(ref _lastLines, obj, lastLine);
        }

        //---------------------------------------

        private static TValue GetValue<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey key)
        {
            if (dictionary == null)
            {
                return default(TValue);
            }
            else
            {
                TValue value;
                dictionary.TryGetValue(key, out value);
                return value;
            }
        }

        private static void SetValue<TKey, TValue>(ref IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary == null)
            {
                dictionary = new Dictionary<TKey, TValue>();
            }

            dictionary[key] = value;
        }
    }
}