// Copyright (c) 2023 LiveAssistant.Protocols.Overlay Authors
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

// ReSharper disable UnassignedField.Global

using System.Text.RegularExpressions;
using Json.Schema;

namespace LiveAssistant.Protocols.Overlay;

public static class Schemas
{
    private static readonly Regex ColorRegex = new(@"^(?:[a-z]|[A-Z]|\d){8}$");

    public static readonly JsonSchema OverlayFieldSchema = new JsonSchemaBuilder().Type(SchemaValueType.Object)
        .Properties(
            (nameof(Models.OverlayFieldPayload.Key).ToCamelCase(), new JsonSchemaBuilder().Type(SchemaValueType.String)),
            (nameof(Models.OverlayFieldPayload.Type).ToCamelCase(), new JsonSchemaBuilder().Type(SchemaValueType.String)
                .Enum(Enum.GetNames(typeof(OverlayFieldType)).Select(t => t.ToCamelCase()))),
            (nameof(Models.OverlayFieldPayload.Name).ToCamelCase(), new JsonSchemaBuilder().Type(SchemaValueType.String)),
            (nameof(Models.OverlayFieldPayload.DefaultValue).ToCamelCase(), new JsonSchemaBuilder().Type(SchemaValueType.String)),
            (nameof(Models.OverlayFieldPayload.Options).ToCamelCase(), new JsonSchemaBuilder().Type(SchemaValueType.Object).PatternProperties(new Dictionary<Regex, JsonSchema>
            {
                { new Regex(@"^(?:[a-z]|[A-Z]|\d|\-|_|\.)+$"), new JsonSchemaBuilder().Type(SchemaValueType.String) }
            })))
        .Required(
            nameof(Models.OverlayFieldPayload.Key).ToCamelCase(),
            nameof(Models.OverlayFieldPayload.Type).ToCamelCase(),
            nameof(Models.OverlayFieldPayload.Name).ToCamelCase())
        .AdditionalProperties(false);

    public static readonly JsonSchema OverlaySchema = new JsonSchemaBuilder().Type(SchemaValueType.Object)
        .Properties(
            (nameof(Models.OverlayPayload.Path).ToCamelCase(), new JsonSchemaBuilder().Type(SchemaValueType.String).Pattern(new Regex(@"^(?:[a-z]|\d|\-){1}(?:[a-z]|\d|\-|\/|\.)+(?:[a-z]|\d|\-){1}$"))),
            (nameof(Models.OverlayPayload.Name).ToCamelCase(), new JsonSchemaBuilder().Type(SchemaValueType.String)),
            (nameof(Models.OverlayPayload.Category).ToCamelCase(), new JsonSchemaBuilder().Type(SchemaValueType.String)),
            (nameof(Models.OverlayPayload.Description).ToCamelCase(), new JsonSchemaBuilder().Type(SchemaValueType.String)),
            (nameof(Models.OverlayPayload.Fields).ToCamelCase(), new JsonSchemaBuilder().Type(SchemaValueType.Array).Items(OverlayFieldSchema)),
            (nameof(Models.OverlayPayload.MinWidth).ToCamelCase(), new JsonSchemaBuilder().Type(SchemaValueType.Number).Minimum(1)),
            (nameof(Models.OverlayPayload.MaxWidth).ToCamelCase(), new JsonSchemaBuilder().Type(SchemaValueType.Number).Minimum(1)),
            (nameof(Models.OverlayPayload.MinHeight).ToCamelCase(), new JsonSchemaBuilder().Type(SchemaValueType.Number).Minimum(1)),
            (nameof(Models.OverlayPayload.MaxHeight).ToCamelCase(), new JsonSchemaBuilder().Type(SchemaValueType.Number).Minimum(1)),
            (nameof(Models.OverlayPayload.PreviewBackgroundColor).ToCamelCase(), new JsonSchemaBuilder().Type(SchemaValueType.String).Pattern(ColorRegex)))
        .Required(
            nameof(Models.OverlayPayload.Path).ToCamelCase(),
            nameof(Models.OverlayPayload.Name).ToCamelCase(),
            nameof(Models.OverlayPayload.Category).ToCamelCase(),
            nameof(Models.OverlayPayload.Fields).ToCamelCase())
        .AdditionalProperties(false);

    public static readonly JsonSchema OverlayProviderSchema = new JsonSchemaBuilder().Type(SchemaValueType.Object)
        .Properties(
            (nameof(Models.OverlayProviderPayload.ProductId).ToCamelCase(), new JsonSchemaBuilder().Type(SchemaValueType.String).Pattern(new Regex(@"^[a-z]{2,10}\.(?:[a-z]|\d){1}(?:[a-z]|\d|\-)*(?:[a-z]|\d){1}\.(?:[a-z]|\d){1}(?:[a-z]|\d|\-|\.)*(?:[a-z]|\d){1}$"))),
            (nameof(Models.OverlayProviderPayload.ProtocolVersion).ToCamelCase(), new JsonSchemaBuilder().Type(SchemaValueType.Number).Maximum(Constants.ProtocolVersion)),
            (nameof(Models.OverlayProviderPayload.Name).ToCamelCase(), new JsonSchemaBuilder().Type(SchemaValueType.String)),
            (nameof(Models.OverlayProviderPayload.BasePath).ToCamelCase(), new JsonSchemaBuilder().Type(SchemaValueType.String).Pattern(new Regex(@"^https?:\/\/\w+(\.\w+)*(:[0-9]+)?\/?(\/[.\w]*)*$"))),
            (nameof(Models.OverlayProviderPayload.Overlays).ToCamelCase(), new JsonSchemaBuilder().Type(SchemaValueType.Array).Items(OverlaySchema)))
        .Required(
            nameof(Models.OverlayProviderPayload.ProductId).ToCamelCase(),
            nameof(Models.OverlayProviderPayload.ProtocolVersion).ToCamelCase(),
            nameof(Models.OverlayProviderPayload.Name).ToCamelCase(),
            nameof(Models.OverlayProviderPayload.Overlays).ToCamelCase())
        .AdditionalProperties(false);
}
