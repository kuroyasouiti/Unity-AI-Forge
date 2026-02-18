using System.Collections.Generic;
using NUnit.Framework;
using MCP.Editor.CodeGen;

namespace MCP.Editor.Tests
{
    /// <summary>
    /// Unit tests for TemplateRenderer.
    /// Tests variable substitution, conditional blocks, and foreach loops.
    /// </summary>
    [TestFixture]
    public class TemplateRendererTests
    {
        #region Variable Substitution

        [Test]
        public void Render_SimpleVariable_ShouldSubstitute()
        {
            var template = "public class {{CLASS_NAME}} {}";
            var vars = new Dictionary<string, object> { { "CLASS_NAME", "PlayerHealth" } };

            var result = TemplateRenderer.Render(template, vars);

            Assert.AreEqual("public class PlayerHealth {}", result);
        }

        [Test]
        public void Render_MultipleVariables_ShouldSubstituteAll()
        {
            var template = "{{A}} + {{B}} = {{C}}";
            var vars = new Dictionary<string, object>
            {
                { "A", "1" },
                { "B", "2" },
                { "C", "3" }
            };

            var result = TemplateRenderer.Render(template, vars);

            Assert.AreEqual("1 + 2 = 3", result);
        }

        [Test]
        public void Render_MissingVariable_ShouldLeaveAsIs()
        {
            var template = "Hello {{NAME}}";
            var vars = new Dictionary<string, object>();

            var result = TemplateRenderer.Render(template, vars);

            Assert.AreEqual("Hello {{NAME}}", result);
        }

        [Test]
        public void Render_FloatVariable_ShouldFormatWithSuffix()
        {
            var template = "float x = {{VALUE}};";
            var vars = new Dictionary<string, object> { { "VALUE", 3.14f } };

            var result = TemplateRenderer.Render(template, vars);

            Assert.AreEqual("float x = 3.14f;", result);
        }

        [Test]
        public void Render_IntVariable_ShouldFormatWithoutSuffix()
        {
            var template = "int x = {{VALUE}};";
            var vars = new Dictionary<string, object> { { "VALUE", 42 } };

            var result = TemplateRenderer.Render(template, vars);

            Assert.AreEqual("int x = 42;", result);
        }

        [Test]
        public void Render_BoolVariable_ShouldFormatLowercase()
        {
            var template = "bool x = {{VALUE}};";
            var vars = new Dictionary<string, object> { { "VALUE", true } };

            var result = TemplateRenderer.Render(template, vars);

            Assert.AreEqual("bool x = true;", result);
        }

        [Test]
        public void Render_NullVariables_ShouldReturnOriginal()
        {
            var template = "Hello {{NAME}}";

            var result = TemplateRenderer.Render(template, null);

            Assert.AreEqual("Hello {{NAME}}", result);
        }

        [Test]
        public void Render_EmptyTemplate_ShouldReturnEmpty()
        {
            var result = TemplateRenderer.Render("", new Dictionary<string, object>());

            Assert.AreEqual("", result);
        }

        [Test]
        public void Render_NullTemplate_ShouldReturnEmpty()
        {
            var result = TemplateRenderer.Render(null, new Dictionary<string, object>());

            Assert.AreEqual("", result);
        }

        #endregion

        #region Conditional Blocks (IF)

        [Test]
        public void Render_IfBlock_TruthyBool_ShouldIncludeContent()
        {
            var template = "before{{#IF SHOW}}inside{{/IF}}after";
            var vars = new Dictionary<string, object> { { "SHOW", true } };

            var result = TemplateRenderer.Render(template, vars);

            Assert.AreEqual("beforeinsideafter", result);
        }

        [Test]
        public void Render_IfBlock_FalsyBool_ShouldExcludeContent()
        {
            var template = "before{{#IF SHOW}}inside{{/IF}}after";
            var vars = new Dictionary<string, object> { { "SHOW", false } };

            var result = TemplateRenderer.Render(template, vars);

            Assert.AreEqual("beforeafter", result);
        }

        [Test]
        public void Render_IfBlock_MissingVariable_ShouldExcludeContent()
        {
            var template = "before{{#IF SHOW}}inside{{/IF}}after";
            var vars = new Dictionary<string, object>();

            var result = TemplateRenderer.Render(template, vars);

            Assert.AreEqual("beforeafter", result);
        }

        [Test]
        public void Render_IfBlock_TruthyInt_ShouldIncludeContent()
        {
            var template = "{{#IF COUNT}}has items{{/IF}}";
            var vars = new Dictionary<string, object> { { "COUNT", 5 } };

            var result = TemplateRenderer.Render(template, vars);

            Assert.AreEqual("has items", result);
        }

        [Test]
        public void Render_IfBlock_ZeroInt_ShouldExcludeContent()
        {
            var template = "{{#IF COUNT}}has items{{/IF}}";
            var vars = new Dictionary<string, object> { { "COUNT", 0 } };

            var result = TemplateRenderer.Render(template, vars);

            Assert.AreEqual("", result);
        }

        [Test]
        public void Render_IfBlock_NonEmptyString_ShouldIncludeContent()
        {
            var template = "{{#IF NAME}}Hello {{NAME}}{{/IF}}";
            var vars = new Dictionary<string, object> { { "NAME", "World" } };

            var result = TemplateRenderer.Render(template, vars);

            Assert.AreEqual("Hello World", result);
        }

        [Test]
        public void Render_IfBlock_EmptyString_ShouldExcludeContent()
        {
            var template = "{{#IF NAME}}Hello{{/IF}}";
            var vars = new Dictionary<string, object> { { "NAME", "" } };

            var result = TemplateRenderer.Render(template, vars);

            Assert.AreEqual("", result);
        }

        [Test]
        public void Render_IfBlock_NullValue_ShouldExcludeContent()
        {
            var template = "{{#IF NAME}}Hello{{/IF}}";
            var vars = new Dictionary<string, object> { { "NAME", null } };

            var result = TemplateRenderer.Render(template, vars);

            Assert.AreEqual("", result);
        }

        [Test]
        public void Render_IfElseBlock_Truthy_ShouldIncludeTrueBlock()
        {
            var template = "{{#IF SHOW}}yes{{#ELSE}}no{{/IF}}";
            var vars = new Dictionary<string, object> { { "SHOW", true } };

            var result = TemplateRenderer.Render(template, vars);

            Assert.AreEqual("yes", result);
        }

        [Test]
        public void Render_IfElseBlock_Falsy_ShouldIncludeElseBlock()
        {
            var template = "{{#IF SHOW}}yes{{#ELSE}}no{{/IF}}";
            var vars = new Dictionary<string, object> { { "SHOW", false } };

            var result = TemplateRenderer.Render(template, vars);

            Assert.AreEqual("no", result);
        }

        [Test]
        public void Render_NegatedIf_TruthyVariable_ShouldExcludeContent()
        {
            var template = "{{#IF !SHOW}}hidden{{/IF}}";
            var vars = new Dictionary<string, object> { { "SHOW", true } };

            var result = TemplateRenderer.Render(template, vars);

            Assert.AreEqual("", result);
        }

        [Test]
        public void Render_NegatedIf_FalsyVariable_ShouldIncludeContent()
        {
            var template = "{{#IF !SHOW}}hidden{{/IF}}";
            var vars = new Dictionary<string, object> { { "SHOW", false } };

            var result = TemplateRenderer.Render(template, vars);

            Assert.AreEqual("hidden", result);
        }

        [Test]
        public void Render_MultipleIfBlocks_ShouldProcessAll()
        {
            var template = "{{#IF A}}aaa{{/IF}}-{{#IF B}}bbb{{/IF}}";
            var vars = new Dictionary<string, object>
            {
                { "A", true },
                { "B", false }
            };

            var result = TemplateRenderer.Render(template, vars);

            Assert.AreEqual("aaa-", result);
        }

        [Test]
        public void Render_IfBlockWithVariables_ShouldSubstituteInside()
        {
            var template = "{{#IF SHOW}}Hello {{NAME}}{{/IF}}";
            var vars = new Dictionary<string, object>
            {
                { "SHOW", true },
                { "NAME", "World" }
            };

            var result = TemplateRenderer.Render(template, vars);

            Assert.AreEqual("Hello World", result);
        }

        #endregion

        #region Foreach Blocks

        [Test]
        public void Render_ForeachBlock_ShouldExpandList()
        {
            var template = "{{#FOREACH ITEMS}}{{NAME}},{{/FOREACH}}";
            var vars = new Dictionary<string, object>
            {
                { "ITEMS", new List<object>
                    {
                        new Dictionary<string, object> { { "NAME", "A" } },
                        new Dictionary<string, object> { { "NAME", "B" } },
                        new Dictionary<string, object> { { "NAME", "C" } }
                    }
                }
            };

            var result = TemplateRenderer.Render(template, vars);

            Assert.AreEqual("A,B,C,", result);
        }

        [Test]
        public void Render_ForeachBlock_EmptyList_ShouldReturnEmpty()
        {
            var template = "before{{#FOREACH ITEMS}}item{{/FOREACH}}after";
            var vars = new Dictionary<string, object>
            {
                { "ITEMS", new List<object>() }
            };

            var result = TemplateRenderer.Render(template, vars);

            Assert.AreEqual("beforeafter", result);
        }

        [Test]
        public void Render_ForeachBlock_MissingVariable_ShouldReturnEmpty()
        {
            var template = "before{{#FOREACH ITEMS}}item{{/FOREACH}}after";
            var vars = new Dictionary<string, object>();

            var result = TemplateRenderer.Render(template, vars);

            Assert.AreEqual("beforeafter", result);
        }

        [Test]
        public void Render_ForeachBlock_WithIndexMetadata()
        {
            var template = "{{#FOREACH ITEMS}}{{_INDEX}},{{/FOREACH}}";
            var vars = new Dictionary<string, object>
            {
                { "ITEMS", new List<object>
                    {
                        new Dictionary<string, object>(),
                        new Dictionary<string, object>(),
                        new Dictionary<string, object>()
                    }
                }
            };

            var result = TemplateRenderer.Render(template, vars);

            Assert.AreEqual("0,1,2,", result);
        }

        [Test]
        public void Render_ForeachBlock_FirstLastMetadata()
        {
            var template = "{{#FOREACH ITEMS}}{{#IF _FIRST}}[{{/IF}}{{NAME}}{{#IF !_LAST}},{{/IF}}{{#IF _LAST}}]{{/IF}}{{/FOREACH}}";
            var vars = new Dictionary<string, object>
            {
                { "ITEMS", new List<object>
                    {
                        new Dictionary<string, object> { { "NAME", "A" } },
                        new Dictionary<string, object> { { "NAME", "B" } },
                        new Dictionary<string, object> { { "NAME", "C" } }
                    }
                }
            };

            var result = TemplateRenderer.Render(template, vars);

            Assert.AreEqual("[A,B,C]", result);
        }

        [Test]
        public void Render_ForeachBlock_ScalarItems()
        {
            var template = "{{#FOREACH TAGS}}\"{{_ITEM}}\",{{/FOREACH}}";
            var vars = new Dictionary<string, object>
            {
                { "TAGS", new List<object> { "Enemy", "Boss", "NPC" } }
            };

            var result = TemplateRenderer.Render(template, vars);

            Assert.AreEqual("\"Enemy\",\"Boss\",\"NPC\",", result);
        }

        [Test]
        public void Render_ForeachBlock_WithIfInside()
        {
            var template = "{{#FOREACH ITEMS}}{{#IF ACTIVE}}+{{NAME}}{{/IF}}{{/FOREACH}}";
            var vars = new Dictionary<string, object>
            {
                { "ITEMS", new List<object>
                    {
                        new Dictionary<string, object> { { "NAME", "A" }, { "ACTIVE", true } },
                        new Dictionary<string, object> { { "NAME", "B" }, { "ACTIVE", false } },
                        new Dictionary<string, object> { { "NAME", "C" }, { "ACTIVE", true } }
                    }
                }
            };

            var result = TemplateRenderer.Render(template, vars);

            Assert.AreEqual("+A+C", result);
        }

        #endregion

        #region Multi-line / Realistic Template

        [Test]
        public void Render_MultilineTemplate_ShouldPreserveFormatting()
        {
            var template = @"public class {{CLASS_NAME}} : MonoBehaviour
{
    [SerializeField] private float maxHealth = {{MAX_HEALTH}};
{{#IF HAS_SHIELD}}
    [SerializeField] private float shield = {{SHIELD}};
{{/IF}}
}";

            var vars = new Dictionary<string, object>
            {
                { "CLASS_NAME", "PlayerHealth" },
                { "MAX_HEALTH", 100f },
                { "HAS_SHIELD", true },
                { "SHIELD", 50f }
            };

            var result = TemplateRenderer.Render(template, vars);

            StringAssert.Contains("public class PlayerHealth : MonoBehaviour", result);
            StringAssert.Contains("maxHealth = 100f", result);
            StringAssert.Contains("shield = 50f", result);
        }

        [Test]
        public void Render_MultilineTemplate_FalseCondition_ShouldExcludeBlock()
        {
            var template = @"public class {{CLASS_NAME}} : MonoBehaviour
{
    [SerializeField] private float maxHealth = {{MAX_HEALTH}};
{{#IF HAS_SHIELD}}
    [SerializeField] private float shield = {{SHIELD}};
{{/IF}}
}";

            var vars = new Dictionary<string, object>
            {
                { "CLASS_NAME", "EnemyHealth" },
                { "MAX_HEALTH", 50f },
                { "HAS_SHIELD", false }
            };

            var result = TemplateRenderer.Render(template, vars);

            StringAssert.Contains("public class EnemyHealth", result);
            StringAssert.Contains("maxHealth = 50f", result);
            StringAssert.DoesNotContain("shield", result);
        }

        #endregion

        #region Float Truthiness

        [Test]
        public void Render_IfBlock_TruthyFloat_ShouldIncludeContent()
        {
            var template = "{{#IF DURATION}}has duration{{/IF}}";
            var vars = new Dictionary<string, object> { { "DURATION", 0.5f } };

            var result = TemplateRenderer.Render(template, vars);

            Assert.AreEqual("has duration", result);
        }

        [Test]
        public void Render_IfBlock_ZeroFloat_ShouldExcludeContent()
        {
            var template = "{{#IF DURATION}}has duration{{/IF}}";
            var vars = new Dictionary<string, object> { { "DURATION", 0f } };

            var result = TemplateRenderer.Render(template, vars);

            Assert.AreEqual("", result);
        }

        #endregion
    }
}
