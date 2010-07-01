using System.CodeDom;

namespace Sage.SalesLogix.Migration.Script
{
    public static class CodeDomUtils
    {
        public static void SetMemberAttributes(CodeTypeMember member, MemberAttributes attributes)
        {
            int mask = 0xF;
            int result = (int) member.Attributes;
            int intAttributes = (int) attributes;

            for (int i = 0; i < 4; i++)
            {
                int part = mask & intAttributes;

                if (part != 0)
                {
                    result = (result & ~mask) | part;
                }

                mask <<= 4;
            }

            member.Attributes = (MemberAttributes) result;
        }

        public static bool AreMemberAttributesSet(CodeTypeMember member, MemberAttributes attributes)
        {
            int mask = 0xF;
            int original = (int) member.Attributes;
            int intAttributes = (int) attributes;

            for (int i = 0; i < 4; i++)
            {
                int part = mask & intAttributes;

                if (part != 0 && part != (mask & original))
                {
                    return false;
                }

                mask <<= 4;
            }

            return true;
        }
    }
}