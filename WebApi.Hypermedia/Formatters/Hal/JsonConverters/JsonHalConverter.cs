﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WebApi.Hypermedia.Formatters.Hal.JsonConverters
{
    public class JsonHalConverter : JsonConverter
    {
        const string StreamingContextResourceConverterToken = "hal+json";
        const StreamingContextStates StreamingContextResourceConverterState = StreamingContextStates.Other;

        public static bool IsResourceConverterContext(StreamingContext context)
        {
            return context.Context is string &&
                   (string)context.Context == StreamingContextResourceConverterToken &&
                   context.State == StreamingContextResourceConverterState;
        }

        private static StreamingContext GetResourceConverterContext()
        {
            return new StreamingContext(StreamingContextResourceConverterState, StreamingContextResourceConverterToken);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var hypermediaData = value as HypermediaObject;
            var saveContext = serializer.Context;
            serializer.Context = GetResourceConverterContext();
            serializer.Converters.Remove(this);
            serializer.Serialize(writer, hypermediaData);
            //writer.WriteStartObject();
            //if (hypermediaData != null)
            //{
            //    writer.WritePropertyName("_links");
            //    serializer.Serialize(writer, hypermediaData.Links);
            //    serializer.Serialize(writer, hypermediaData.Dto);
            //}
            //writer.WriteEndObject();
            serializer.Converters.Add(this);
            serializer.Context = saveContext;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
                                        JsonSerializer serializer)
        {
            var hypermediaData = new HypermediaObject();
            JObject jsonObject = JObject.Load(reader);
            serializer.Populate(jsonObject.CreateReader(), hypermediaData);
            return hypermediaData;
            //JObject jsonObject = JObject.Load(reader);
            //var properties = jsonObject.Properties().ToList();
            //return new HypermediaObject((object) properties[0].Value);
        }

        const string HalLinksName = "_links";
        const string HalEmbeddedName = "_embedded";

        //static IResource CreateResource(JObject jObj, Type resourceType)
        //{
        //    // remove _links and _embedded so those don't try to deserialize, because we know they will fail
        //    JToken links;
        //    if (jObj.TryGetValue(HalLinksName, out links))
        //        jObj.Remove(HalLinksName);
        //    JToken embeddeds;
        //    if (jObj.TryGetValue(HalEmbeddedName, out embeddeds))
        //        jObj.Remove(HalEmbeddedName);

        //    // create value properties in base object
        //    var resource = jObj.ToObject(resourceType) as IResource;
        //    if (resource == null) return null;

        //    // links are named properties, where the name is Link.Rel and the value is the rest of Link
        //    if (links != null)
        //    {
        //        foreach (var rel in links.OfType<JProperty>())
        //            CreateLinks(rel, resource);
        //        var self = resource.Links.SingleOrDefault(l => l.Rel == "self");
        //        if (self != null)
        //            resource.Href = self.Href;
        //    }

        //    // embedded are named properties, where the name is the Rel, which needs to map to a Resource Type, and the value is the Resource
        //    // recursive
        //    if (embeddeds != null)
        //    {
        //        foreach (var prop in resourceType.GetProperties().Where(p => Representation.IsEmbeddedResourceType(p.PropertyType)))
        //        {
        //            var propertyRelAttribute = prop.GetCustomAttribute<RelAttribute>(true);

        //            // expects embedded collection of resources is implemented as an IList on the Representation-derived class
        //            var lst = prop.GetValue(resource) as IList;
        //            if (lst != null)
        //            {
        //                if (prop.PropertyType.GenericTypeArguments != null &&
        //                    prop.PropertyType.GenericTypeArguments.Length > 0)
        //                {
        //                    if (propertyRelAttribute != null && !String.IsNullOrWhiteSpace(propertyRelAttribute.Rel))
        //                    {
        //                        CreateEmbedded(embeddeds, prop.PropertyType.GenericTypeArguments[0], newRes => lst.Add(newRes), propertyRelAttribute.Rel);
        //                    }
        //                    else if (
        //                        prop.PropertyType.GenericTypeArguments[0].GetInterfaces()
        //                            .Contains(typeof(IResource)))
        //                    {
        //                        CreateEmbedded(embeddeds, prop.PropertyType.GenericTypeArguments[0], newRes => lst.Add(newRes), lst);
        //                    }
        //                    else
        //                    {
        //                        CreateEmbedded(embeddeds, prop.PropertyType.GenericTypeArguments[0], newRes => lst.Add(newRes));
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                var val = prop.GetValue(resource) as IResource;
        //                if (val != null)
        //                {
        //                    CreateEmbedded(embeddeds, prop.PropertyType, newRes => prop.SetValue(resource, newRes), val);
        //                }
        //                else
        //                {
        //                    CreateEmbedded(embeddeds, prop.PropertyType, newRes => prop.SetValue(resource, newRes));
        //                }
        //            }
        //        }
        //    }

        //    return resource;
        //}

        //static void CreateLinks(JProperty rel, IResource resource)
        //{
        //    if (rel.Value.Type == JTokenType.Array)
        //    {
        //        var arr = rel.Value as JArray;
        //        if (arr != null)
        //            foreach (var link in arr.Select(item => item.ToObject<Link>()))
        //            {
        //                link.Rel = rel.Name;
        //                resource.Links.Add(link);
        //            }
        //    }
        //    else
        //    {
        //        var link = rel.Value.ToObject<Link>();
        //        link.Rel = rel.Name;
        //        resource.Links.Add(link);
        //    }
        //}

        //static void CreateEmbedded(JToken embeddeds, Type resourceType, Action<IResource> addCreatedResource, IList resourcePropertyList)
        //{
        //    if (resourcePropertyList != null && resourcePropertyList.Count > 0 && (resourcePropertyList[0] as IResource) != null)
        //    {
        //        var firstResource = resourcePropertyList[0] as IResource;
        //        if (!String.IsNullOrWhiteSpace(firstResource.Rel))
        //            CreateEmbedded(embeddeds, resourceType, addCreatedResource, firstResource.Rel);
        //        else
        //        {
        //            CreateEmbedded(embeddeds, resourceType, addCreatedResource);
        //        }
        //    }
        //    else
        //    {
        //        if (resourcePropertyList != null)
        //        {
        //            var resourceRel = GetResourceTypeRel(resourcePropertyList.GetType().GenericTypeArguments[0]);
        //            if (!String.IsNullOrWhiteSpace(resourceRel))
        //            {
        //                resourceRel = null;
        //            }
        //            CreateEmbedded(embeddeds, resourceType, addCreatedResource, resourceRel);
        //        }
        //    }
        //}
        //static void CreateEmbedded(JToken embeddeds, Type resourceType, Action<IResource> addCreatedResource, IResource resourcePropertyValue)
        //{
        //    if (resourcePropertyValue != null && !String.IsNullOrWhiteSpace(resourcePropertyValue.Rel))
        //    {
        //        CreateEmbedded(embeddeds, resourceType, addCreatedResource, resourcePropertyValue.Rel);
        //    }
        //}
        //static void CreateEmbedded(JToken embeddeds, Type resourceType, Action<IResource> addCreatedResource, string rel = null)
        //{
        //    if (rel == null)
        //        rel = GetResourceTypeRel(resourceType);

        //    if (!string.IsNullOrEmpty(rel))
        //    {
        //        var tok = embeddeds[rel];
        //        if (tok != null)
        //        {
        //            switch (tok.Type)
        //            {
        //                case JTokenType.Array:
        //                    {
        //                        var embeddedJArr = tok as JArray;
        //                        if (embeddedJArr != null)
        //                        {
        //                            foreach (var embeddedJObj in embeddedJArr.OfType<JObject>())
        //                                addCreatedResource(CreateResource(embeddedJObj, resourceType)); // recursion
        //                        }
        //                    }
        //                    break;
        //                case JTokenType.Object:
        //                    {
        //                        var embeddedJObj = tok as JObject;
        //                        if (embeddedJObj != null)
        //                            addCreatedResource(CreateResource(embeddedJObj, resourceType)); // recursion
        //                    }
        //                    break;
        //            }
        //        }
        //    }
        //}

        //// this depends on IResource.Rel being set upon construction
        //static readonly IDictionary<string, string> ResourceTypeToRel = new Dictionary<string, string>();
        //static readonly object ResourceTypeToRelLock = new object();
        //static string GetResourceTypeRel(Type resourceType)
        //{
        //    if (ResourceTypeToRel.ContainsKey(resourceType.FullName))
        //        return ResourceTypeToRel[resourceType.FullName];
        //    try
        //    {
        //        lock (ResourceTypeToRelLock)
        //        {
        //            if (ResourceTypeToRel.ContainsKey(resourceType.FullName))
        //                return ResourceTypeToRel[resourceType.FullName];
        //            // favor c-tor with zero params, but if it doesn't exist, use c-tor with fewest params and pass all null values
        //            var ctors = resourceType.GetConstructors();
        //            ConstructorInfo useThisCtor = null;
        //            foreach (var ctor in ctors)
        //            {
        //                if (ctor.GetParameters().Length == 0)
        //                {
        //                    useThisCtor = ctor;
        //                    break;
        //                }
        //                if (useThisCtor == null || useThisCtor.GetParameters().Length > ctor.GetParameters().Length)
        //                    useThisCtor = ctor;
        //            }
        //            if (useThisCtor == null) return string.Empty;
        //            var ctorParams = new object[useThisCtor.GetParameters().Length];
        //            var res = useThisCtor.Invoke(ctorParams) as IResource;
        //            if (res != null)
        //            {
        //                var rel = res.Rel;
        //                ResourceTypeToRel.Add(resourceType.FullName, rel);
        //                return rel;
        //            }
        //        }
        //        return string.Empty;
        //    }
        //    catch
        //    {
        //        return string.Empty;
        //    }
        //}

        public override bool CanConvert(Type objectType)
        {
            return IsResource(objectType); // && !IsResourceList(objectType);
        }

        //static bool IsResourceList(Type objectType)
        //{
        //    return typeof(IRepresentationList).IsAssignableFrom(objectType);
        //}

        static bool IsResource(Type objectType)
        {
            return typeof(HypermediaObject).IsAssignableFrom(objectType);
        }
    }
}
