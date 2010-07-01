using System;
using System.CodeDom;
using System.ComponentModel;

namespace Sage.SalesLogix.Migration.Script.CodeDom
{
    [Serializable]
    public sealed class CodeSwitchStatement : CodeConditionStatement
    {
        private CodeExpression _target;
        private readonly CodeSwitchOptionCollection _options;
        private CodeStatementCollection _defaultStatements;
        private CodeSwitchOption _firstOption;

        public CodeSwitchStatement()
        {
            _options = new CodeSwitchOptionCollection(OptionsChanged);
            _defaultStatements = base.FalseStatements;
            Refresh();
        }

        public CodeSwitchStatement(CodeExpression target, params CodeSwitchOption[] options)
        {
            _target = target;
            _options = new CodeSwitchOptionCollection(OptionsChanged, options);

            foreach (CodeSwitchOption option in options)
            {
                option.SetTarget(_target);
            }

            _defaultStatements = base.FalseStatements;
            Refresh();
        }

        public CodeSwitchStatement(CodeExpression target, CodeSwitchOption[] options, params CodeStatement[] defaultStatements)
        {
            _target = target;
            _options = new CodeSwitchOptionCollection(OptionsChanged, options);

            foreach (CodeSwitchOption option in options)
            {
                option.SetTarget(_target);
            }

            _defaultStatements = base.FalseStatements;
            _defaultStatements.AddRange(defaultStatements);
            Refresh();
        }

        public CodeExpression Target
        {
            get { return _target; }
            set
            {
                if (_target != value)
                {
                    _target = value;
                    Refresh();
                }
            }
        }

        public CodeSwitchOptionCollection Options
        {
            get { return _options; }
        }

        public CodeStatementCollection DefaultStatements
        {
            get { return _defaultStatements; }
        }

        [Obsolete]
        public new CodeExpression Condition
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        [Obsolete]
        public new CodeStatementCollection TrueStatements
        {
            get { throw new NotSupportedException(); }
        }

        [Obsolete]
        public new CodeStatementCollection FalseStatements
        {
            get { throw new NotSupportedException(); }
        }

        private void OptionsChanged(CollectionChangeAction action, CodeSwitchOption option)
        {
            if (action == CollectionChangeAction.Add)
            {
                option.SetTarget(_target);
            }
            else if (action == CollectionChangeAction.Remove)
            {
                option.SetTarget(null);
            }

            Refresh();
        }

        private void Refresh()
        {
            int optionCount = _options.Count;
            CodeSwitchOption firstOption = (optionCount > 0
                                                ? _options[0]
                                                : null);

            if (_firstOption != firstOption)
            {
                if (_firstOption != null)
                {
                    _firstOption.SetActualStmt(null);
                }

                _firstOption = firstOption;

                if (_firstOption != null)
                {
                    _firstOption.SetActualStmt(this);
                }
            }

            CodeStatementCollection defaultStatements = (optionCount > 1
                                                             ? ((CodeConditionStatement) _options[optionCount - 1]).FalseStatements
                                                             : base.FalseStatements);

            if (_defaultStatements != defaultStatements)
            {
                defaultStatements.Clear();

                if (_defaultStatements != null)
                {
                    defaultStatements.AddRange(_defaultStatements);
                    _defaultStatements.Clear();
                }

                _defaultStatements = defaultStatements;
            }

            CodeConditionStatement conditionStmt = this;

            foreach (CodeConditionStatement option in _options)
            {
                if (option != firstOption)
                {
                    conditionStmt.FalseStatements.Clear();
                    conditionStmt.FalseStatements.Add(option);
                    conditionStmt = option;
                }
            }

            if (conditionStmt == this)
            {
                base.Condition = new CodePrimitiveExpression(false);
            }
        }
    }
}