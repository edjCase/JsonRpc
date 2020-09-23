using System;
using System.IO;
using System.Reflection;
using System.Xml.XPath;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EdjCase.JsonRpc.Router.Swagger
{
    public interface IXmlDocumentationService
    {
        string GetSummuryForType(Type type);
        string GetSummuryForMethod(MethodInfo methodInfo);
        string GetMethodParameterExample(MethodInfo methodInfo, ParameterInfo parameterInfo);
        string GetPropertyExample(PropertyInfo propertyInfo);
    }

    public class XmlDocumentationService : IXmlDocumentationService
    {
        private XPathNavigator xpathNavigator;
        private const string MemberXPath = "/doc/members/member[@name='{0}']";
        private const string SummaryTag = "summary";

        public XmlDocumentationService()
        {
            var filePath = Path.Combine(System.AppContext.BaseDirectory, $"{Assembly.GetEntryAssembly().GetName().Name}.xml");
            if (File.Exists(filePath))
            {
                var xmlComments = File.OpenText(filePath);
                var xpathDocument = new XPathDocument(xmlComments);
                this.xpathNavigator = xpathDocument.CreateNavigator();
            }
            else
            {
                var xpathDocument = new XPathDocument(new StringReader("<none></none>"));
                this.xpathNavigator = xpathDocument.CreateNavigator();
            }
        }
        
        public string GetSummuryForType(Type type)
        {
            var memberName = XmlCommentsNodeNameHelper.GetMemberNameForType(type);
            var typeNode = this.xpathNavigator.SelectSingleNode(string.Format(XmlDocumentationService.MemberXPath, memberName));
            if (typeNode == null) return string.Empty;
            var summaryNode = typeNode.SelectSingleNode(XmlDocumentationService.SummaryTag);
            return XmlCommentsTextHelper.Humanize(summaryNode.InnerXml);
        }

        public string GetSummuryForMethod(MethodInfo methodInfo)
        {
            var methodMemberName = XmlCommentsNodeNameHelper.GetMemberNameForMethod(methodInfo);
            var methodNode = this.xpathNavigator.SelectSingleNode($"/doc/members/member[@name='{methodMemberName}']");
            var summaryNode = methodNode?.SelectSingleNode("summary");
            return summaryNode != null ? XmlCommentsTextHelper.Humanize(summaryNode.InnerXml) : string.Empty;
        }

        public string GetMethodParameterExample(MethodInfo methodInfo, ParameterInfo parameterInfo)
        {
            var methodMemberName = XmlCommentsNodeNameHelper.GetMemberNameForMethod(methodInfo);
            var paramNode = this.xpathNavigator.SelectSingleNode(
                $"/doc/members/member[@name='{methodMemberName}']/param[@name='{parameterInfo.Name}']");
            if (paramNode == null) return string.Empty;
            var example = paramNode.GetAttribute("example", "");
            return example;
        }

        public string GetPropertyExample(PropertyInfo propertyInfo)
        {
            var propertyMemberName = XmlCommentsNodeNameHelper.GetMemberNameForFieldOrProperty(propertyInfo);
            var propertyExampleNode = this.xpathNavigator.SelectSingleNode($"/doc/members/member[@name='{propertyMemberName}']/example");
            if (propertyExampleNode == null) return string.Empty;
            var example = XmlCommentsTextHelper.Humanize(propertyExampleNode.InnerXml);
            return example;
        }
    }
}