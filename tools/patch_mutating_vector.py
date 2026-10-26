"""Defensive bounds guards and tracing for the 16-byte render list.
The source of invalid entries is still under investigation; callback mutation
is a hypothesis, not an established cause.
"""
from pathlib import Path

root = Path(__file__).resolve().parents[1] / "generated" / "game"
guards = {
    "000BFAE4": (0, 0),
    "000BFB89": (16, 0),
    "000BFC2B": (32, 0),
    "000BFCD1": (48, 0),
    "000BFDA3": (0, 1),
}
for name in ("x86_recomp.31.cpp", "x86_recomp.32.cpp"):
    path = root / name
    source = path.read_text()
    for label, (advance, tail) in guards.items():
        anchor = f"loc_{label}:\n"
        if anchor not in source:
            if label == "000BFAE4" and name.endswith("31.cpp"):
                continue
            raise RuntimeError(f"Missing {label} in {name}")
        marker = f"// Azurik vector mutation guard {label}"
        if marker in source:
            continue
        index = "X86_MEM32(ctx.ebp + 0xFFFFFFF8u)" if tail else "X86_MEM32(ctx.ebp + 0xFFFFFFFCu)"
        guard = (
            f"\t{marker}\n"
            f"\t{{ const uint32_t begin = X86_MEM32(ctx.edi), end = X86_MEM32(ctx.edi + 4u);\n"
            f"\t  const uint32_t offset = uint32_t({index}) + {advance}u;\n"
            f"\t  if (!begin || end < begin || offset > end - begin || end - begin - offset < 16u)\n"
            f"\t      goto loc_000BFE58; }}\n"
        )
        source = source.replace(anchor, anchor + guard, 1)
    # A live vector slot can itself be cleared during a transition. Skip that
    # item before the original code dereferences its object at +0xB8.
    null_anchor = "\t// 000BFAEF mov ecx, [eax+0xB8]\n"
    null_marker = "// Azurik empty render-item guard 000BFAEF"
    if null_anchor not in source:
        raise RuntimeError(f"Missing 000BFAEF in {name}")
    if null_marker not in source:
        source = source.replace(null_anchor,
            "\t" + null_marker + "\n"
            "\tif (ctx.eax < 0x10000u || ctx.eax >= 0x20000000u || (ctx.eax & 3u)) goto loc_000BFB89;\n" + null_anchor, 1)
    else:
        source = source.replace("if (!ctx.eax) goto loc_000BFB89;",
                                "if (ctx.eax < 0x10000u || ctx.eax >= 0x20000000u || (ctx.eax & 3u)) goto loc_000BFB89;")
        source = source.replace("if (ctx.eax < 0x10000u || (ctx.eax & 3u)) goto loc_000BFB89;",
                                "if (ctx.eax < 0x10000u || ctx.eax >= 0x20000000u || (ctx.eax & 3u)) goto loc_000BFB89;")
    for address, reg, next_label in (
        ("000BFB96", "edx", "000BFC2B"),
        ("000BFC3B", "ecx", "000BFCD1"),
        ("000BFCE1", "ecx", "000BFD76"),
        ("000BFDAD", "ecx", "000BFE42"),
    ):
        if address == "000BFCE1" and name.endswith("31.cpp"):
            continue
        anchor = f"\t// {address} test [{reg}+0xB8], "
        marker = f"// Azurik empty render-item guard {address}"
        if anchor not in source:
            raise RuntimeError(f"Missing {address} in {name}")
        if marker not in source:
            source = source.replace(anchor,
                f"\t{marker}\n\tif (ctx.{reg} < 0x10000u || ctx.{reg} >= 0x20000000u || (ctx.{reg} & 3u)) goto loc_{next_label};\n" + anchor, 1)
        else:
            source = source.replace(f"if (ctx.{reg} < 0x10000u || (ctx.{reg} & 3u)) goto loc_{next_label};",
                                    f"if (ctx.{reg} < 0x10000u || ctx.{reg} >= 0x20000000u || (ctx.{reg} & 3u)) goto loc_{next_label};")
    for address, offset, next_label in (
        ("000BFB80", "X86_MEM32(ctx.ebp + 0xFFFFFFFCu)", "000BFB89"),
        ("000BFC22", "X86_MEM32(ctx.ebp + 0xFFFFFFFCu) + 16u", "000BFC2B"),
        ("000BFCC8", "X86_MEM32(ctx.ebp + 0xFFFFFFFCu) + 32u", "000BFCD1"),
        ("000BFD6A", "X86_MEM32(ctx.ebp + 0xFFFFFFFCu) + 48u", "000BFD76"),
        ("000BFE36", "X86_MEM32(ctx.ebp + 0xFFFFFFF8u)", "000BFE42"),
    ):
        if address == "000BFD6A" and name.endswith("31.cpp"):
            continue
        anchor = f"loc_{address}:\n"
        marker = f"// Azurik refreshed render-item guard {address}"
        if anchor not in source:
            raise RuntimeError(f"Missing {address} in {name}")
        if marker not in source:
            guard = (f"\t{marker}\n"
                     f"\t{{ const uint32_t begin = X86_MEM32(ctx.edi), end = X86_MEM32(ctx.edi + 4u);\n"
                     f"\t  const uint32_t off = uint32_t({offset});\n"
                     f"\t  if (!begin || end < begin || off > end - begin || end - begin - off < 16u) goto loc_000BFE58;\n"
                     f"\t  ctx.ebx = begin + off; const uint32_t object = X86_MEM32(ctx.ebx);\n"
                     f"\t  if (object < 0x10000u || object >= 0x20000000u || (object & 3u)) goto loc_{next_label}; }}\n")
            source = source.replace(anchor, anchor + guard, 1)
        else:
            source = source.replace(f"if (object < 0x10000u || (object & 3u)) goto loc_{next_label};",
                                    f"if (object < 0x10000u || object >= 0x20000000u || (object & 3u)) goto loc_{next_label};")
    if name.endswith("32.cpp"):
        anchor = "loc_000BFCD9:\n"
        marker = "// Azurik fourth render-item guard 000BFCD9"
        if marker not in source:
            guard = ("\t" + marker + "\n"
                     "\t{ const uint32_t begin = X86_MEM32(ctx.edi), end = X86_MEM32(ctx.edi + 4u);\n"
                     "\t  const uint32_t off = uint32_t(X86_MEM32(ctx.ebp + 0xFFFFFFFCu)) + 48u;\n"
                     "\t  if (!begin || end < begin || off > end - begin || end - begin - off < 16u) goto loc_000BFE58;\n"
                     "\t  ctx.ebx = begin; }\n")
            source = source.replace(anchor, anchor + guard, 1)
    if path.read_text() != source:
        path.write_text(source)

# The guards above are the production fix.  Earlier diagnostic builds wrapped
# every virtual render call and copied whole lists to locate the mutation; that
# work is deliberately omitted here because it causes frame-time spikes.

