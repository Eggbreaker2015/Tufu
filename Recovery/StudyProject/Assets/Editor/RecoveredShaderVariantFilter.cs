using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

// Generated source has arithmetic only for original APK permutations. Remove
// impossible Cartesian products of keyword groups before Unity compiles them.
public sealed class RecoveredShaderVariantFilter : IPreprocessShaders
{
    [Serializable] private class Pass {public string shader,passName,passType;public int subshader,pass;public string[] keys,variants;}
    [Serializable] private class Data {public Pass[] passes;}
    private static Data data;
    public int callbackOrder=>0;
    public void OnProcessShader(Shader shader,ShaderSnippetData snippet,IList<ShaderCompilerData> variants)
    {
        if(data==null)
        {
            string path=Path.Combine(Application.dataPath,"Editor/RecoveredShaderVariants.json");
            if(!File.Exists(path))throw new FileNotFoundException("Original APK shader permutation map is required for a recovered shader build",path);
            data=JsonUtility.FromJson<Data>(File.ReadAllText(path));
        }
        // Pass names/types repeat between native SubShaders with different supported
        // keywords. Match the actual identifier, otherwise a less capable SubShader
        // can incorrectly admit a keyword it never declared (e.g. SHADOWS_SOFT).
        var passes=data.passes.Where(p=>p.shader==shader.name && p.subshader==(int)snippet.pass.SubshaderIndex && p.pass==(int)snippet.pass.PassIndex && p.passName==snippet.passName && p.passType==snippet.passType.ToString()).ToArray();
        if(passes.Length==0)return;
        int original=variants.Count;
        for(int i=variants.Count-1;i>=0;i--)
        {
            var set=variants[i].shaderKeywordSet;
            bool supported=passes.Any(p=>p.variants.Contains(string.Join("|",p.keys.Where(k=>set.IsEnabled(new LocalKeyword(shader,k))))));
            if(!supported)variants.RemoveAt(i);
        }
        Debug.Log("RECOVERED_SHADER_VARIANTS "+shader.name+"/"+snippet.pass.SubshaderIndex+":"+snippet.pass.PassIndex+"/"+snippet.passType+"/"+snippet.shaderType+" kept="+variants.Count+" removed="+(original-variants.Count));
    }
}
