using System;
using UnityEngine;

public static class FlowLeniaImporter
{
    // Converts a Lenia "free-kernel" JSON to FlowLeniaModel.
    // JSON (LeniaF lineage):
    //   { "model":  {R,T,P,kn,gn},
    //     "params": [{ m,s,h, c0,c1, rings:[{r,w,b:[...]},...] }, ...] }
    // Legacy "c" => c0=c1=c handled.

    public static FlowLeniaModel ImportJson(string jsonText)
    {
        if (string.IsNullOrWhiteSpace(jsonText))
            throw new Exception("Empty JSON text");

        FlowLeniaJsonRoot root;
        try { root = JsonUtility.FromJson<FlowLeniaJsonRoot>(jsonText); }
        catch (Exception ex) { throw new Exception("JSON parse error: " + ex.Message); }

        if (root == null)           throw new Exception("Invalid JSON: root is null");
        if (root.model == null)     throw new Exception("Invalid JSON: 'model' missing");
        if (root.@params == null)   throw new Exception("Invalid JSON: 'params' missing");

        var asset = ScriptableObject.CreateInstance<FlowLeniaModel>();
        asset.R  = Mathf.Max(1, root.model.R);
        asset.T  = root.model.T;
        asset.P  = root.model.P;
        asset.kn = Mathf.Max(1, root.model.kn);
        asset.gn = Mathf.Max(1, root.model.gn);

        foreach (var p in root.@params)
        {
            if (p == null) continue;
            var k = new FlowKernel();
            k.mu    = p.m;
            k.sigma = Mathf.Max(1e-6f, p.s);
            k.h     = p.h;

            // upgrade legacy single c
            if (p.c0 == 0 && p.c1 == 0 && p.c != 0) { k.c0 = p.c; k.c1 = p.c; }
            else { k.c0 = p.c0; k.c1 = p.c1; }

            if (p.rings != null) {
                foreach (var r in p.rings) {
                    if (r == null) continue;
                    var rr = new FlowRing {
                        r = Mathf.Max(0, r.r),
                        w = Mathf.Max(0, r.w),
                        b = (r.b != null && r.b.Count > 0)
                              ? new System.Collections.Generic.List<float>(r.b)
                              : new System.Collections.Generic.List<float>(){1f}
                    };
                    k.rings.Add(rr);
                }
            }
            asset.kernels.Add(k);
        }
        return asset;
    }
}
