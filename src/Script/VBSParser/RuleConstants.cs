namespace Sage.SalesLogix.Migration.Script.VBSParser
{
    public enum RuleConstants
    {
        NLNewLine = 0, // <NL> ::= NewLine <NL>
        NLNewLine2 = 1, // <NL> ::= NewLine
        Program = 2, // <Program> ::= <NLOpt> <GlobalStmtList>
        ClassDeclClassEndClass = 3, // <ClassDecl> ::= Class <ExtendedID> <NL> <MemberDeclList> End Class <NL>
        MemberDeclList = 4, // <MemberDeclList> ::= <MemberDecl> <MemberDeclList>
        MemberDeclList2 = 5, // <MemberDeclList> ::= 
        MemberDecl = 6, // <MemberDecl> ::= <FieldDecl>
        MemberDecl2 = 7, // <MemberDecl> ::= <VarDecl>
        MemberDecl3 = 8, // <MemberDecl> ::= <ConstDecl>
        MemberDecl4 = 9, // <MemberDecl> ::= <SubDecl>
        MemberDecl5 = 10, // <MemberDecl> ::= <FunctionDecl>
        MemberDecl6 = 11, // <MemberDecl> ::= <PropertyDecl>
        FieldDeclPrivate = 12, // <FieldDecl> ::= Private <FieldName> <OtherVarsOpt> <NL>
        FieldDeclPublic = 13, // <FieldDecl> ::= Public <FieldName> <OtherVarsOpt> <NL>
        FieldNameLParanRParan = 14, // <FieldName> ::= <FieldID> '(' <ArrayRankList> ')'
        FieldName = 15, // <FieldName> ::= <FieldID>
        FieldIDID = 16, // <FieldID> ::= ID
        FieldIDDefault = 17, // <FieldID> ::= Default
        FieldIDErase = 18, // <FieldID> ::= Erase
        FieldIDError = 19, // <FieldID> ::= Error
        FieldIDExplicit = 20, // <FieldID> ::= Explicit
        FieldIDStep = 21, // <FieldID> ::= Step
        VarDeclDim = 22, // <VarDecl> ::= Dim <VarName> <OtherVarsOpt> <NL>
        VarNameLParanRParan = 23, // <VarName> ::= <ExtendedID> '(' <ArrayRankList> ')'
        VarName = 24, // <VarName> ::= <ExtendedID>
        OtherVarsOptComma = 25, // <OtherVarsOpt> ::= ',' <VarName> <OtherVarsOpt>
        OtherVarsOpt = 26, // <OtherVarsOpt> ::= 
        ArrayRankListComma = 27, // <ArrayRankList> ::= <IntLiteral> ',' <ArrayRankList>
        ArrayRankList = 28, // <ArrayRankList> ::= <IntLiteral>
        ArrayRankList2 = 29, // <ArrayRankList> ::= 
        ConstDeclConst = 30, // <ConstDecl> ::= <AccessModifierOpt> Const <ConstList> <NL>
        ConstListEqComma = 31, // <ConstList> ::= <ExtendedID> '=' <ConstExprDef> ',' <ConstList>
        ConstListEq = 32, // <ConstList> ::= <ExtendedID> '=' <ConstExprDef>
        ConstExprDefLParanRParan = 33, // <ConstExprDef> ::= '(' <ConstExprDef> ')'
        ConstExprDefMinus = 34, // <ConstExprDef> ::= '-' <ConstExprDef>
        ConstExprDefPlus = 35, // <ConstExprDef> ::= '+' <ConstExprDef>
        ConstExprDef = 36, // <ConstExprDef> ::= <ConstExpr>
        SubDeclSubEndSub = 37, // <SubDecl> ::= <MethodAccessOpt> Sub <ExtendedID> <MethodArgList> <NL> <MethodStmtList> End Sub <NL>
        SubDeclSubEndSub2 = 38, // <SubDecl> ::= <MethodAccessOpt> Sub <ExtendedID> <MethodArgList> <InlineStmt> End Sub <NL>
        FunctionDeclFunctionEndFunction = 39, // <FunctionDecl> ::= <MethodAccessOpt> Function <ExtendedID> <MethodArgList> <NL> <MethodStmtList> End Function <NL>
        FunctionDeclFunctionEndFunction2 = 40, // <FunctionDecl> ::= <MethodAccessOpt> Function <ExtendedID> <MethodArgList> <InlineStmt> End Function <NL>
        MethodAccessOptPublicDefault = 41, // <MethodAccessOpt> ::= Public Default
        MethodAccessOpt = 42, // <MethodAccessOpt> ::= <AccessModifierOpt>
        AccessModifierOptPublic = 43, // <AccessModifierOpt> ::= Public
        AccessModifierOptPrivate = 44, // <AccessModifierOpt> ::= Private
        AccessModifierOpt = 45, // <AccessModifierOpt> ::= 
        MethodArgListLParanRParan = 46, // <MethodArgList> ::= '(' <ArgList> ')'
        MethodArgListLParanRParan2 = 47, // <MethodArgList> ::= '(' ')'
        MethodArgList = 48, // <MethodArgList> ::= 
        ArgListComma = 49, // <ArgList> ::= <Arg> ',' <ArgList>
        ArgList = 50, // <ArgList> ::= <Arg>
        ArgLParanRParan = 51, // <Arg> ::= <ArgModifierOpt> <ExtendedID> '(' ')'
        Arg = 52, // <Arg> ::= <ArgModifierOpt> <ExtendedID>
        ArgModifierOptByVal = 53, // <ArgModifierOpt> ::= ByVal
        ArgModifierOptByRef = 54, // <ArgModifierOpt> ::= ByRef
        ArgModifierOpt = 55, // <ArgModifierOpt> ::= 
        PropertyDeclPropertyEndProperty = 56, // <PropertyDecl> ::= <MethodAccessOpt> Property <PropertyAccessType> <ExtendedID> <MethodArgList> <NL> <MethodStmtList> End Property <NL>
        PropertyAccessTypeGet = 57, // <PropertyAccessType> ::= Get
        PropertyAccessTypeLet = 58, // <PropertyAccessType> ::= Let
        PropertyAccessTypeSet = 59, // <PropertyAccessType> ::= Set
        GlobalStmt = 60, // <GlobalStmt> ::= <OptionExplicit>
        GlobalStmt2 = 61, // <GlobalStmt> ::= <ClassDecl>
        GlobalStmt3 = 62, // <GlobalStmt> ::= <FieldDecl>
        GlobalStmt4 = 63, // <GlobalStmt> ::= <ConstDecl>
        GlobalStmt5 = 64, // <GlobalStmt> ::= <SubDecl>
        GlobalStmt6 = 65, // <GlobalStmt> ::= <FunctionDecl>
        GlobalStmt7 = 66, // <GlobalStmt> ::= <BlockStmt>
        MethodStmt = 67, // <MethodStmt> ::= <ConstDecl>
        MethodStmt2 = 68, // <MethodStmt> ::= <BlockStmt>
        BlockStmt = 69, // <BlockStmt> ::= <VarDecl>
        BlockStmt2 = 70, // <BlockStmt> ::= <RedimStmt>
        BlockStmt3 = 71, // <BlockStmt> ::= <IfStmt>
        BlockStmt4 = 72, // <BlockStmt> ::= <WithStmt>
        BlockStmt5 = 73, // <BlockStmt> ::= <SelectStmt>
        BlockStmt6 = 74, // <BlockStmt> ::= <LoopStmt>
        BlockStmt7 = 75, // <BlockStmt> ::= <ForStmt>
        BlockStmt8 = 76, // <BlockStmt> ::= <InlineStmt> <NL>
        InlineStmt = 77, // <InlineStmt> ::= <AssignStmt>
        InlineStmt2 = 78, // <InlineStmt> ::= <CallStmt>
        InlineStmt3 = 79, // <InlineStmt> ::= <SubCallStmt>
        InlineStmt4 = 80, // <InlineStmt> ::= <ErrorStmt>
        InlineStmt5 = 81, // <InlineStmt> ::= <ExitStmt>
        InlineStmtErase = 82, // <InlineStmt> ::= Erase <ExtendedID>
        GlobalStmtList = 83, // <GlobalStmtList> ::= <GlobalStmt> <GlobalStmtList>
        GlobalStmtList2 = 84, // <GlobalStmtList> ::= 
        MethodStmtList = 85, // <MethodStmtList> ::= <MethodStmt> <MethodStmtList>
        MethodStmtList2 = 86, // <MethodStmtList> ::= 
        BlockStmtList = 87, // <BlockStmtList> ::= <BlockStmt> <BlockStmtList>
        BlockStmtList2 = 88, // <BlockStmtList> ::= 
        OptionExplicitOptionExplicit = 89, // <OptionExplicit> ::= Option Explicit <NL>
        ErrorStmtOnErrorResumeNext = 90, // <ErrorStmt> ::= On Error Resume Next
        ErrorStmtOnErrorGoToIntLiteral = 91, // <ErrorStmt> ::= On Error GoTo IntLiteral
        ExitStmtExitDo = 92, // <ExitStmt> ::= Exit Do
        ExitStmtExitFor = 93, // <ExitStmt> ::= Exit For
        ExitStmtExitFunction = 94, // <ExitStmt> ::= Exit Function
        ExitStmtExitProperty = 95, // <ExitStmt> ::= Exit Property
        ExitStmtExitSub = 96, // <ExitStmt> ::= Exit Sub
        AssignStmtEq = 97, // <AssignStmt> ::= <LeftExpr> '=' <Expr>
        AssignStmtSetEq = 98, // <AssignStmt> ::= Set <LeftExpr> '=' <Expr>
        SubCallStmt = 99, // <SubCallStmt> ::= <QualifiedID> <SubSafeExprOpt> <CommaExprList>
        SubCallStmt2 = 100, // <SubCallStmt> ::= <QualifiedID> <SubSafeExprOpt>
        SubCallStmtLParanRParan = 101, // <SubCallStmt> ::= <QualifiedID> '(' <Expr> ')' <CommaExprList>
        SubCallStmtLParanRParan2 = 102, // <SubCallStmt> ::= <QualifiedID> '(' <Expr> ')'
        SubCallStmtLParanRParan3 = 103, // <SubCallStmt> ::= <QualifiedID> '(' ')'
        SubCallStmtDot = 104, // <SubCallStmt> ::= <QualifiedID> <IndexOrParamsList> '.' <LeftExprTail> <SubSafeExprOpt> <CommaExprList>
        SubCallStmt3 = 105, // <SubCallStmt> ::= <QualifiedID> <IndexOrParamsListDot> <LeftExprTail> <SubSafeExprOpt> <CommaExprList>
        SubCallStmtDot2 = 106, // <SubCallStmt> ::= <QualifiedID> <IndexOrParamsList> '.' <LeftExprTail> <SubSafeExprOpt>
        SubCallStmt4 = 107, // <SubCallStmt> ::= <QualifiedID> <IndexOrParamsListDot> <LeftExprTail> <SubSafeExprOpt>
        SubSafeExprOpt = 108, // <SubSafeExprOpt> ::= <SubSafeExpr>
        SubSafeExprOpt2 = 109, // <SubSafeExprOpt> ::= 
        CallStmtCall = 110, // <CallStmt> ::= Call <LeftExpr>
        LeftExprDot = 111, // <LeftExpr> ::= <QualifiedID> <IndexOrParamsList> '.' <LeftExprTail>
        LeftExpr = 112, // <LeftExpr> ::= <QualifiedID> <IndexOrParamsListDot> <LeftExprTail>
        LeftExpr2 = 113, // <LeftExpr> ::= <QualifiedID> <IndexOrParamsList>
        LeftExpr3 = 114, // <LeftExpr> ::= <QualifiedID>
        LeftExpr4 = 115, // <LeftExpr> ::= <SafeKeywordID>
        LeftExprTailDot = 116, // <LeftExprTail> ::= <QualifiedIDTail> <IndexOrParamsList> '.' <LeftExprTail>
        LeftExprTail = 117, // <LeftExprTail> ::= <QualifiedIDTail> <IndexOrParamsListDot> <LeftExprTail>
        LeftExprTail2 = 118, // <LeftExprTail> ::= <QualifiedIDTail> <IndexOrParamsList>
        LeftExprTail3 = 119, // <LeftExprTail> ::= <QualifiedIDTail>
        QualifiedIDIDDot = 120, // <QualifiedID> ::= IDDot <QualifiedIDTail>
        QualifiedIDDotIDDot = 121, // <QualifiedID> ::= DotIDDot <QualifiedIDTail>
        QualifiedIDID = 122, // <QualifiedID> ::= ID
        QualifiedIDDotID = 123, // <QualifiedID> ::= DotID
        QualifiedIDTailIDDot = 124, // <QualifiedIDTail> ::= IDDot <QualifiedIDTail>
        QualifiedIDTailID = 125, // <QualifiedIDTail> ::= ID
        QualifiedIDTail = 126, // <QualifiedIDTail> ::= <KeywordID>
        KeywordID = 127, // <KeywordID> ::= <SafeKeywordID>
        KeywordIDAnd = 128, // <KeywordID> ::= And
        KeywordIDByRef = 129, // <KeywordID> ::= ByRef
        KeywordIDByVal = 130, // <KeywordID> ::= ByVal
        KeywordIDCall = 131, // <KeywordID> ::= Call
        KeywordIDCase = 132, // <KeywordID> ::= Case
        KeywordIDClass = 133, // <KeywordID> ::= Class
        KeywordIDConst = 134, // <KeywordID> ::= Const
        KeywordIDDim = 135, // <KeywordID> ::= Dim
        KeywordIDDo = 136, // <KeywordID> ::= Do
        KeywordIDEach = 137, // <KeywordID> ::= Each
        KeywordIDElse = 138, // <KeywordID> ::= Else
        KeywordIDElseIf = 139, // <KeywordID> ::= ElseIf
        KeywordIDEmpty = 140, // <KeywordID> ::= Empty
        KeywordIDEnd = 141, // <KeywordID> ::= End
        KeywordIDEqv = 142, // <KeywordID> ::= Eqv
        KeywordIDExit = 143, // <KeywordID> ::= Exit
        KeywordIDFalse = 144, // <KeywordID> ::= False
        KeywordIDFor = 145, // <KeywordID> ::= For
        KeywordIDFunction = 146, // <KeywordID> ::= Function
        KeywordIDGet = 147, // <KeywordID> ::= Get
        KeywordIDGoTo = 148, // <KeywordID> ::= GoTo
        KeywordIDIf = 149, // <KeywordID> ::= If
        KeywordIDImp = 150, // <KeywordID> ::= Imp
        KeywordIDIn = 151, // <KeywordID> ::= In
        KeywordIDIs = 152, // <KeywordID> ::= Is
        KeywordIDLet = 153, // <KeywordID> ::= Let
        KeywordIDLoop = 154, // <KeywordID> ::= Loop
        KeywordIDMod = 155, // <KeywordID> ::= Mod
        KeywordIDNew = 156, // <KeywordID> ::= New
        KeywordIDNext = 157, // <KeywordID> ::= Next
        KeywordIDNot = 158, // <KeywordID> ::= Not
        KeywordIDNothing = 159, // <KeywordID> ::= Nothing
        KeywordIDNull = 160, // <KeywordID> ::= Null
        KeywordIDOn = 161, // <KeywordID> ::= On
        KeywordIDOption = 162, // <KeywordID> ::= Option
        KeywordIDOr = 163, // <KeywordID> ::= Or
        KeywordIDPreserve = 164, // <KeywordID> ::= Preserve
        KeywordIDPrivate = 165, // <KeywordID> ::= Private
        KeywordIDPublic = 166, // <KeywordID> ::= Public
        KeywordIDRedim = 167, // <KeywordID> ::= Redim
        KeywordIDResume = 168, // <KeywordID> ::= Resume
        KeywordIDSelect = 169, // <KeywordID> ::= Select
        KeywordIDSet = 170, // <KeywordID> ::= Set
        KeywordIDSub = 171, // <KeywordID> ::= Sub
        KeywordIDThen = 172, // <KeywordID> ::= Then
        KeywordIDTo = 173, // <KeywordID> ::= To
        KeywordIDTrue = 174, // <KeywordID> ::= True
        KeywordIDUntil = 175, // <KeywordID> ::= Until
        KeywordIDWEnd = 176, // <KeywordID> ::= WEnd
        KeywordIDWhile = 177, // <KeywordID> ::= While
        KeywordIDWith = 178, // <KeywordID> ::= With
        KeywordIDXor = 179, // <KeywordID> ::= Xor
        SafeKeywordIDDefault = 180, // <SafeKeywordID> ::= Default
        SafeKeywordIDErase = 181, // <SafeKeywordID> ::= Erase
        SafeKeywordIDError = 182, // <SafeKeywordID> ::= Error
        SafeKeywordIDExplicit = 183, // <SafeKeywordID> ::= Explicit
        SafeKeywordIDProperty = 184, // <SafeKeywordID> ::= Property
        SafeKeywordIDStep = 185, // <SafeKeywordID> ::= Step
        ExtendedID = 186, // <ExtendedID> ::= <SafeKeywordID>
        ExtendedIDID = 187, // <ExtendedID> ::= ID
        IndexOrParamsList = 188, // <IndexOrParamsList> ::= <IndexOrParams> <IndexOrParamsList>
        IndexOrParamsList2 = 189, // <IndexOrParamsList> ::= <IndexOrParams>
        IndexOrParamsLParanRParan = 190, // <IndexOrParams> ::= '(' <Expr> <CommaExprList> ')'
        IndexOrParamsLParanRParan2 = 191, // <IndexOrParams> ::= '(' <CommaExprList> ')'
        IndexOrParamsLParanRParan3 = 192, // <IndexOrParams> ::= '(' <Expr> ')'
        IndexOrParamsLParanRParan4 = 193, // <IndexOrParams> ::= '(' ')'
        IndexOrParamsListDot = 194, // <IndexOrParamsListDot> ::= <IndexOrParams> <IndexOrParamsListDot>
        IndexOrParamsListDot2 = 195, // <IndexOrParamsListDot> ::= <IndexOrParamsDot>
        IndexOrParamsDotLParanRParanDot = 196, // <IndexOrParamsDot> ::= '(' <Expr> <CommaExprList> ').'
        IndexOrParamsDotLParanRParanDot2 = 197, // <IndexOrParamsDot> ::= '(' <CommaExprList> ').'
        IndexOrParamsDotLParanRParanDot3 = 198, // <IndexOrParamsDot> ::= '(' <Expr> ').'
        IndexOrParamsDotLParanRParanDot4 = 199, // <IndexOrParamsDot> ::= '(' ').'
        CommaExprListComma = 200, // <CommaExprList> ::= ',' <Expr> <CommaExprList>
        CommaExprListComma2 = 201, // <CommaExprList> ::= ',' <CommaExprList>
        CommaExprListComma3 = 202, // <CommaExprList> ::= ',' <Expr>
        CommaExprListComma4 = 203, // <CommaExprList> ::= ','
        RedimStmtRedim = 204, // <RedimStmt> ::= Redim <RedimDeclList> <NL>
        RedimStmtRedimPreserve = 205, // <RedimStmt> ::= Redim Preserve <RedimDeclList> <NL>
        RedimDeclListComma = 206, // <RedimDeclList> ::= <RedimDecl> ',' <RedimDeclList>
        RedimDeclList = 207, // <RedimDeclList> ::= <RedimDecl>
        RedimDeclLParanRParan = 208, // <RedimDecl> ::= <ExtendedID> '(' <ExprList> ')'
        IfStmtIfThenEndIf = 209, // <IfStmt> ::= If <Expr> Then <NL> <BlockStmtList> <ElseStmtList> End If <NL>
        IfStmtIfThen = 210, // <IfStmt> ::= If <Expr> Then <InlineStmt> <ElseOpt> <EndIfOpt> <NL>
        ElseStmtListElseIfThen = 211, // <ElseStmtList> ::= ElseIf <Expr> Then <NL> <BlockStmtList> <ElseStmtList>
        ElseStmtListElseIfThen2 = 212, // <ElseStmtList> ::= ElseIf <Expr> Then <InlineStmt> <NL> <ElseStmtList>
        ElseStmtListElse = 213, // <ElseStmtList> ::= Else <InlineStmt> <NL>
        ElseStmtListElse2 = 214, // <ElseStmtList> ::= Else <NL> <BlockStmtList>
        ElseStmtList = 215, // <ElseStmtList> ::= 
        ElseOptElse = 216, // <ElseOpt> ::= Else <InlineStmt>
        ElseOpt = 217, // <ElseOpt> ::= 
        EndIfOptEndIf = 218, // <EndIfOpt> ::= End If
        EndIfOpt = 219, // <EndIfOpt> ::= 
        WithStmtWithEndWith = 220, // <WithStmt> ::= With <Expr> <NL> <BlockStmtList> End With <NL>
        LoopStmtDoLoop = 221, // <LoopStmt> ::= Do <LoopType> <Expr> <NL> <BlockStmtList> Loop <NL>
        LoopStmtDoLoop2 = 222, // <LoopStmt> ::= Do <NL> <BlockStmtList> Loop <LoopType> <Expr> <NL>
        LoopStmtDoLoop3 = 223, // <LoopStmt> ::= Do <NL> <BlockStmtList> Loop <NL>
        LoopStmtWhileWEnd = 224, // <LoopStmt> ::= While <Expr> <NL> <BlockStmtList> WEnd <NL>
        LoopTypeWhile = 225, // <LoopType> ::= While
        LoopTypeUntil = 226, // <LoopType> ::= Until
        ForStmtForEqToNext = 227, // <ForStmt> ::= For <ExtendedID> '=' <Expr> To <Expr> <StepOpt> <NL> <BlockStmtList> Next <NL>
        ForStmtForEachInNext = 228, // <ForStmt> ::= For Each <ExtendedID> In <Expr> <NL> <BlockStmtList> Next <NL>
        StepOptStep = 229, // <StepOpt> ::= Step <Expr>
        StepOpt = 230, // <StepOpt> ::= 
        SelectStmtSelectCaseEndSelect = 231, // <SelectStmt> ::= Select Case <Expr> <NL> <CaseStmtList> End Select <NL>
        CaseStmtListCase = 232, // <CaseStmtList> ::= Case <ExprList> <NLOpt> <BlockStmtList> <CaseStmtList>
        CaseStmtListCaseElse = 233, // <CaseStmtList> ::= Case Else <NLOpt> <BlockStmtList>
        CaseStmtList = 234, // <CaseStmtList> ::= 
        NLOpt = 235, // <NLOpt> ::= <NL>
        NLOpt2 = 236, // <NLOpt> ::= 
        ExprListComma = 237, // <ExprList> ::= <Expr> ',' <ExprList>
        ExprList = 238, // <ExprList> ::= <Expr>
        SubSafeExpr = 239, // <SubSafeExpr> ::= <SubSafeImpExpr>
        SubSafeImpExprImp = 240, // <SubSafeImpExpr> ::= <SubSafeImpExpr> Imp <EqvExpr>
        SubSafeImpExpr = 241, // <SubSafeImpExpr> ::= <SubSafeEqvExpr>
        SubSafeEqvExprEqv = 242, // <SubSafeEqvExpr> ::= <SubSafeEqvExpr> Eqv <XorExpr>
        SubSafeEqvExpr = 243, // <SubSafeEqvExpr> ::= <SubSafeXorExpr>
        SubSafeXorExprXor = 244, // <SubSafeXorExpr> ::= <SubSafeXorExpr> Xor <OrExpr>
        SubSafeXorExpr = 245, // <SubSafeXorExpr> ::= <SubSafeOrExpr>
        SubSafeOrExprOr = 246, // <SubSafeOrExpr> ::= <SubSafeOrExpr> Or <AndExpr>
        SubSafeOrExpr = 247, // <SubSafeOrExpr> ::= <SubSafeAndExpr>
        SubSafeAndExprAnd = 248, // <SubSafeAndExpr> ::= <SubSafeAndExpr> And <NotExpr>
        SubSafeAndExpr = 249, // <SubSafeAndExpr> ::= <SubSafeNotExpr>
        SubSafeNotExprNot = 250, // <SubSafeNotExpr> ::= Not <NotExpr>
        SubSafeNotExpr = 251, // <SubSafeNotExpr> ::= <SubSafeCompareExpr>
        SubSafeCompareExprIs = 252, // <SubSafeCompareExpr> ::= <SubSafeCompareExpr> Is <ConcatExpr>
        SubSafeCompareExprIsNot = 253, // <SubSafeCompareExpr> ::= <SubSafeCompareExpr> Is Not <ConcatExpr>
        SubSafeCompareExprGtEq = 254, // <SubSafeCompareExpr> ::= <SubSafeCompareExpr> '>=' <ConcatExpr>
        SubSafeCompareExprEqGt = 255, // <SubSafeCompareExpr> ::= <SubSafeCompareExpr> '=>' <ConcatExpr>
        SubSafeCompareExprLtEq = 256, // <SubSafeCompareExpr> ::= <SubSafeCompareExpr> '<=' <ConcatExpr>
        SubSafeCompareExprEqLt = 257, // <SubSafeCompareExpr> ::= <SubSafeCompareExpr> '=<' <ConcatExpr>
        SubSafeCompareExprGt = 258, // <SubSafeCompareExpr> ::= <SubSafeCompareExpr> '>' <ConcatExpr>
        SubSafeCompareExprLt = 259, // <SubSafeCompareExpr> ::= <SubSafeCompareExpr> '<' <ConcatExpr>
        SubSafeCompareExprLtGt = 260, // <SubSafeCompareExpr> ::= <SubSafeCompareExpr> '<>' <ConcatExpr>
        SubSafeCompareExprEq = 261, // <SubSafeCompareExpr> ::= <SubSafeCompareExpr> '=' <ConcatExpr>
        SubSafeCompareExpr = 262, // <SubSafeCompareExpr> ::= <SubSafeConcatExpr>
        SubSafeConcatExprAmp = 263, // <SubSafeConcatExpr> ::= <SubSafeConcatExpr> '&' <AddExpr>
        SubSafeConcatExpr = 264, // <SubSafeConcatExpr> ::= <SubSafeAddExpr>
        SubSafeAddExprPlus = 265, // <SubSafeAddExpr> ::= <SubSafeAddExpr> '+' <ModExpr>
        SubSafeAddExprMinus = 266, // <SubSafeAddExpr> ::= <SubSafeAddExpr> '-' <ModExpr>
        SubSafeAddExpr = 267, // <SubSafeAddExpr> ::= <SubSafeModExpr>
        SubSafeModExprMod = 268, // <SubSafeModExpr> ::= <SubSafeModExpr> Mod <IntDivExpr>
        SubSafeModExpr = 269, // <SubSafeModExpr> ::= <SubSafeIntDivExpr>
        SubSafeIntDivExprBackslash = 270, // <SubSafeIntDivExpr> ::= <SubSafeIntDivExpr> '\' <MultExpr>
        SubSafeIntDivExpr = 271, // <SubSafeIntDivExpr> ::= <SubSafeMultExpr>
        SubSafeMultExprTimes = 272, // <SubSafeMultExpr> ::= <SubSafeMultExpr> '*' <UnaryExpr>
        SubSafeMultExprDiv = 273, // <SubSafeMultExpr> ::= <SubSafeMultExpr> '/' <UnaryExpr>
        SubSafeMultExpr = 274, // <SubSafeMultExpr> ::= <SubSafeUnaryExpr>
        SubSafeUnaryExprMinus = 275, // <SubSafeUnaryExpr> ::= '-' <UnaryExpr>
        SubSafeUnaryExprPlus = 276, // <SubSafeUnaryExpr> ::= '+' <UnaryExpr>
        SubSafeUnaryExpr = 277, // <SubSafeUnaryExpr> ::= <SubSafeExpExpr>
        SubSafeExpExprCaret = 278, // <SubSafeExpExpr> ::= <SubSafeValue> '^' <ExpExpr>
        SubSafeExpExpr = 279, // <SubSafeExpExpr> ::= <SubSafeValue>
        SubSafeValue = 280, // <SubSafeValue> ::= <ConstExpr>
        SubSafeValue2 = 281, // <SubSafeValue> ::= <LeftExpr>
        SubSafeValueNew = 282, // <SubSafeValue> ::= New <LeftExpr>
        Expr = 283, // <Expr> ::= <ImpExpr>
        ImpExprImp = 284, // <ImpExpr> ::= <ImpExpr> Imp <EqvExpr>
        ImpExpr = 285, // <ImpExpr> ::= <EqvExpr>
        EqvExprEqv = 286, // <EqvExpr> ::= <EqvExpr> Eqv <XorExpr>
        EqvExpr = 287, // <EqvExpr> ::= <XorExpr>
        XorExprXor = 288, // <XorExpr> ::= <XorExpr> Xor <OrExpr>
        XorExpr = 289, // <XorExpr> ::= <OrExpr>
        OrExprOr = 290, // <OrExpr> ::= <OrExpr> Or <AndExpr>
        OrExpr = 291, // <OrExpr> ::= <AndExpr>
        AndExprAnd = 292, // <AndExpr> ::= <AndExpr> And <NotExpr>
        AndExpr = 293, // <AndExpr> ::= <NotExpr>
        NotExprNot = 294, // <NotExpr> ::= Not <NotExpr>
        NotExpr = 295, // <NotExpr> ::= <CompareExpr>
        CompareExprIs = 296, // <CompareExpr> ::= <CompareExpr> Is <ConcatExpr>
        CompareExprIsNot = 297, // <CompareExpr> ::= <CompareExpr> Is Not <ConcatExpr>
        CompareExprGtEq = 298, // <CompareExpr> ::= <CompareExpr> '>=' <ConcatExpr>
        CompareExprEqGt = 299, // <CompareExpr> ::= <CompareExpr> '=>' <ConcatExpr>
        CompareExprLtEq = 300, // <CompareExpr> ::= <CompareExpr> '<=' <ConcatExpr>
        CompareExprEqLt = 301, // <CompareExpr> ::= <CompareExpr> '=<' <ConcatExpr>
        CompareExprGt = 302, // <CompareExpr> ::= <CompareExpr> '>' <ConcatExpr>
        CompareExprLt = 303, // <CompareExpr> ::= <CompareExpr> '<' <ConcatExpr>
        CompareExprLtGt = 304, // <CompareExpr> ::= <CompareExpr> '<>' <ConcatExpr>
        CompareExprEq = 305, // <CompareExpr> ::= <CompareExpr> '=' <ConcatExpr>
        CompareExpr = 306, // <CompareExpr> ::= <ConcatExpr>
        ConcatExprAmp = 307, // <ConcatExpr> ::= <ConcatExpr> '&' <AddExpr>
        ConcatExpr = 308, // <ConcatExpr> ::= <AddExpr>
        AddExprPlus = 309, // <AddExpr> ::= <AddExpr> '+' <ModExpr>
        AddExprMinus = 310, // <AddExpr> ::= <AddExpr> '-' <ModExpr>
        AddExpr = 311, // <AddExpr> ::= <ModExpr>
        ModExprMod = 312, // <ModExpr> ::= <ModExpr> Mod <IntDivExpr>
        ModExpr = 313, // <ModExpr> ::= <IntDivExpr>
        IntDivExprBackslash = 314, // <IntDivExpr> ::= <IntDivExpr> '\' <MultExpr>
        IntDivExpr = 315, // <IntDivExpr> ::= <MultExpr>
        MultExprTimes = 316, // <MultExpr> ::= <MultExpr> '*' <UnaryExpr>
        MultExprDiv = 317, // <MultExpr> ::= <MultExpr> '/' <UnaryExpr>
        MultExpr = 318, // <MultExpr> ::= <UnaryExpr>
        UnaryExprMinus = 319, // <UnaryExpr> ::= '-' <UnaryExpr>
        UnaryExprPlus = 320, // <UnaryExpr> ::= '+' <UnaryExpr>
        UnaryExpr = 321, // <UnaryExpr> ::= <ExpExpr>
        ExpExprCaret = 322, // <ExpExpr> ::= <Value> '^' <ExpExpr>
        ExpExpr = 323, // <ExpExpr> ::= <Value>
        Value = 324, // <Value> ::= <ConstExpr>
        Value2 = 325, // <Value> ::= <LeftExpr>
        ValueLParanRParan = 326, // <Value> ::= '(' <Expr> ')'
        ValueNew = 327, // <Value> ::= New <LeftExpr>
        ConstExpr = 328, // <ConstExpr> ::= <BoolLiteral>
        ConstExpr2 = 329, // <ConstExpr> ::= <IntLiteral>
        ConstExprFloatLiteral = 330, // <ConstExpr> ::= FloatLiteral
        ConstExprStringLiteral = 331, // <ConstExpr> ::= StringLiteral
        ConstExprDateLiteral = 332, // <ConstExpr> ::= DateLiteral
        ConstExpr3 = 333, // <ConstExpr> ::= <Nothing>
        BoolLiteralTrue = 334, // <BoolLiteral> ::= True
        BoolLiteralFalse = 335, // <BoolLiteral> ::= False
        IntLiteralIntLiteral = 336, // <IntLiteral> ::= IntLiteral
        IntLiteralHexLiteral = 337, // <IntLiteral> ::= HexLiteral
        IntLiteralOctLiteral = 338, // <IntLiteral> ::= OctLiteral
        NothingNothing = 339, // <Nothing> ::= Nothing
        NothingNull = 340, // <Nothing> ::= Null
        NothingEmpty = 341 // <Nothing> ::= Empty
    }
}