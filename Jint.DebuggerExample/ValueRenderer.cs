using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json;
using Jint.Native;
using Jint.Native.Function;
using Jint.Native.Object;
using JintDebuggerExample.Helpers;

namespace Jint.DebuggerExample;

/// <summary>
/// A somewhat minimal approach to yielding useful output about variables, properties and objects.
/// Jint.DebugAdapter has a much more complete example of handling various types of values.
/// </summary>
internal class ValueRenderer
{
    private static readonly JsonSerializerOptions stringToJsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public string RenderBinding(string name, JsValue? value)
    {
        string valueString = RenderValue(value);
        return RenderBinding(name, valueString);
    }

    // Although strings are implicitly converted to JsString, we don't want literal strings (e.g. "(...)")
    // JSON encoded, like JsString is - hence this overload of RenderBinding
    public string RenderBinding(string name, string value)
    {
        string croppedName = name.CropEnd(20);
        string croppedValue = value.CropEnd(55);
        return $"{croppedName,-20} : {croppedValue,-55}";
    }

    public string RenderValue(JsValue? value, bool renderProperties = false)
    {
        return value switch
        {
            null => "null",
            JsString => JsonSerializer.Serialize(value.ToString(), stringToJsonOptions),
            Function func => RenderFunction(func),
            ObjectInstance obj => renderProperties ? RenderObject(obj) : obj.ToString(),
            _ => value.ToString()
        };
    }

    private string RenderObject(ObjectInstance obj)
    {
        var result = new List<string>();
        foreach (var prop in obj.GetOwnProperties())
        {
            string name = prop.Key.ToString();
            if (prop.Value.Get != null)
            {
                result.Add(RenderBinding(name, "(...)"));
            }
            else
            {
                result.Add(RenderBinding(name, prop.Value.Value));
            }
        }

        // Let's also output getters of the prototype chain
        var proto = obj.Prototype;
        while (proto != null && proto is not ObjectConstructor)
        {
            var props = proto.GetOwnProperties();
            foreach (var prop in props)
            {
                if (prop.Value.Get != null)
                {
                    result.Add(RenderBinding(prop.Key.ToString(), "(...)"));
                }
            }
            proto = proto.Prototype;
        }

        return String.Join(Environment.NewLine, result);
    }

    private string RenderFunction(Function func)
    {
        string result = func.ToString();

        if (result.StartsWith("function "))
        {
            result = string.Concat("ƒ ", result.AsSpan("function ".Length));
        }

        return result;
    }
}
