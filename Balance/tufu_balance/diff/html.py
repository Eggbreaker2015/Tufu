"""Offline, deterministic designer view of a diff comparison."""
from collections import Counter, defaultdict
from html import escape
import math
import statistics

from . import tables


def e(value):
    return escape(tables.text(value), quote=True)


def num(value, digits=1):
    if not isinstance(value, (int, float)) or isinstance(value, bool) or not math.isfinite(value):
        return '—'
    return f'{value:,.{digits}f}'


def pct(a, b):
    return (b / a - 1) * 100 if isinstance(a, (int, float)) and isinstance(b, (int, float)) and a else None


def signed(v):
    return f'{v:+,.1f}%' if v is not None and math.isfinite(v) else '—'


def section(title, body):
    return f'<section><h2>{e(title)}</h2>{body}</section>'


def empty(reason):
    return f'<p class="muted">{e(reason)}</p>'


def spawners(comp):
    a = {r['spawner']: r for r in comp.bake_base}
    b = {r['spawner']: r for r in comp.bake_head}
    ot = {r['spawner']: r for r in comp.static.base.tables['optimum_time']}
    changed = {r['key'][0] for r in comp.comparisons['bake/spawners.csv']}
    result = []
    for sid in sorted(changed):
        x, y = a.get(sid, {}), b.get(sid, {})
        result.append(dict(id=sid, island=ot.get(sid, {}).get('location', '?'),
                           day0=x.get('day', x.get('recalc_optimum_time')), day1=y.get('day', y.get('recalc_optimum_time')),
                           hp0=x.get('new_health'), hp1=y.get('new_health'),
                           damage0=x.get('new_damage'), damage1=y.get('new_damage')))
    return result


def tooltip(r):
    return (f"{r['id']} · 岛 {r['island']}\n烘焙日 {num(r['day0'])} → {num(r['day1'])}"
            f"\nHP {num(r['hp0'])} → {num(r['hp1'])} ({signed(pct(r['hp0'], r['hp1']))})"
            f"\n伤害 {num(r['damage0'])} → {num(r['damage1'])} ({signed(pct(r['damage0'], r['damage1']))})")


def scatter(rows, metric, label):
    groups = defaultdict(list)
    for r in rows:
        y = pct(r[metric+'0'], r[metric+'1'])
        x = r['day0']
        if isinstance(x, (int, float)) and y is not None and math.isfinite(x) and math.isfinite(y):
            groups[(x, y)].append(r)
    if not groups:
        return empty(f'{label}没有可计算的变化比例（基线可能为零或缺失）。')
    xs = [k[0] for k in groups]; ys = [k[1] for k in groups] + [0]
    xmin, xmax = min(xs), max(xs); ymin, ymax = min(ys), max(ys)
    if xmin == xmax: xmin -= 1; xmax += 1
    if ymin == ymax: ymin -= 1; ymax += 1
    xp = lambda x: 55 + 610*(x-xmin)/(xmax-xmin)
    yp = lambda y: 220 - 185*(y-ymin)/(ymax-ymin)
    parts = [f'<h3>{e(label)}变化</h3><svg viewBox="0 0 700 265" role="img" aria-label="{e(label)}变化散点图">',
             f'<line class="zero" x1="55" x2="665" y1="{yp(0):.2f}" y2="{yp(0):.2f}"/>',
             f'<text x="5" y="{yp(0):.2f}">0%</text><text x="55" y="250">烘焙日 {num(xmin)} → {num(xmax)}</text>',
             f'<text x="5" y="25">{e(signed(ymax))}</text><text x="5" y="220">{e(signed(ymin))}</text>']
    colors = ['#1261a0','#bd4f1b','#18805a','#9a3c91','#886500','#4657b0','#a63248','#167c80']
    for (x,y), members in sorted(groups.items()):
        islands = sorted({str(r['island']) for r in members})
        color = colors[sum(map(ord, islands[0])) % len(colors)] if len(islands) == 1 else '#555555'
        tip = '\n\n'.join(tooltip(r) for r in members)
        parts.append(f'<circle tabindex="0" cx="{xp(x):.2f}" cy="{yp(y):.2f}" r="{5 if len(members)==1 else 8}" fill="{color}"><title>{e(tip)}</title></circle>')
        if len(members)>1:
            parts.append(f'<text class="count" x="{xp(x):.2f}" y="{yp(y)+3:.2f}">{len(members)}</text>')
    parts.append('</svg><p class="muted">颜色区分岛；跨岛重叠点为灰色，重叠点显示数量。悬停或聚焦查看详情。</p>')
    islands = sorted({str(r['island']) for r in rows})
    parts.append('<p class="legend">' + ' '.join(f'<span><i style="background:{colors[sum(map(ord, island)) % len(colors)]}"></i>岛 {e(island)}</span>' for island in islands) + '</p>')
    return ''.join(parts)


def ranges(rows):
    groups = defaultdict(list)
    for r in rows:
        for metric in ('hp', 'damage'):
            p = pct(r[metric+'0'], r[metric+'1'])
            if p is not None and math.isfinite(p) and r[metric+'0'] != r[metric+'1']: groups[(str(r['island']), metric)].append(p)
    if not groups: return empty('没有可计算的刷怪点变化范围。')
    lo = min([0]+[min(v) for v in groups.values()]); hi = max([0]+[max(v) for v in groups.values()])
    if lo == hi: lo -= 1; hi += 1
    x = lambda v: 160+490*(v-lo)/(hi-lo)
    height = 35+len(groups)*30
    parts = [f'<svg viewBox="0 0 700 {height}" role="img" aria-label="各岛变化范围">',f'<line class="zero" x1="{x(0):.2f}" x2="{x(0):.2f}" y1="10" y2="{height-10}"/>']
    for i, ((island, metric), vals) in enumerate(sorted(groups.items())):
        y = 22+i*30; a, m, b = min(vals), statistics.median(vals), max(vals)
        parts.append(f'<text x="5" y="{y+4}">岛 {e(island)} · {"HP" if metric == "hp" else "伤害"}</text><line class="range" x1="{x(a):.2f}" x2="{x(b):.2f}" y1="{y}" y2="{y}"/><circle cx="{x(m):.2f}" cy="{y}" r="5" fill="#1261a0"><title>{e(signed(a))} / {e(signed(m))} / {e(signed(b))}</title></circle>')
    return ''.join(parts)+f'</svg><p class="muted">最小值 — 中位数 — 最大值；范围 {e(signed(lo))} 至 {e(signed(hi))}。</p>'


def key_label(path, key):
    if 'evolution_target_days' in path and len(key) >= 3:
        rarities = ['普通','优秀','稀有','史诗','传说','神圣','神话']
        rarity = key[1]
        return f'{"武器" if key[0] == "weapon" else "护甲" if key[0] == "armor" else key[0]} · {rarities[rarity] if isinstance(rarity, int) and 0 <= rarity < 7 else rarity} · 第 {key[2]} 档'
    return tables.text(key)


def static_chart(path, rows):
    expanded = []
    for r in rows:
        if isinstance(r.get('base'), str) and isinstance(r.get('patched'), str) and ';' in r['base']:
            try:
                a = [float(v.strip()) for v in r['base'].split(';')]
                b = [float(v.strip()) for v in r['patched'].split(';')]
                if len(a) == len(b):
                    expanded.extend({**r, 'field': str(r['field']) + f' · {i+1}', 'base': x, 'patched': y} for i, (x, y) in enumerate(zip(a,b)))
                    continue
            except ValueError:
                pass
        expanded.append(r)
    rows = expanded
    numeric = [r for r in rows if all(isinstance(r.get(k), (int,float)) and math.isfinite(r[k]) for k in ('base','patched'))]
    name = path.removeprefix('static/').removesuffix('.csv')
    title = {'island_target_days':'岛目标天数', 'evolution_target_days':'进化目标天数', 'optimum_time':'刷怪点最佳时间', 'interpolation_points':'插值节点', 'relative_windows':'岛时间窗口', 'snapshot_composition':'快照构成', 'bake':'烘焙', 'asset_leaves':'资产改动', 'evolution_values':'进化数值', 'evolution_islands':'进化目标岛', 'amulet_level_days':'护符等级天数', 'bosses':'首领', 'item_evolve_jumps':'物品进化跳跃', 'spawner_runtime':'刷怪点运行数值'}.get(name, name)
    if not numeric:
        return f'<h3>{e(title)}</h3>'+''.join(f'<p>{e(key_label(path, r["key"]))} · {e(r["field"])}：{e(r["base"])} → {e(r["patched"])}</p>' for r in rows)
    if len(numeric) > 20:
        vals = [r['patched']-r['base'] for r in numeric]; lo, hi = min(vals+[0]), max(vals+[0]); span = hi-lo or 1
        bins = [0]*10
        for v in vals: bins[min(9,int((v-lo)/span*10))] += 1
        mx = max(bins) or 1
        bars = ''.join(f'<rect x="{50+i*60}" y="{180-140*n/mx:.2f}" width="48" height="{140*n/mx:.2f}"><title>{n} 项</title></rect>' for i,n in enumerate(bins))
        return f'<h3>{e(title)} · 差值分布</h3><svg viewBox="0 0 700 220" role="img" aria-label="差值分布">{bars}<text x="50" y="210">{num(lo)} → {num(hi)}（{len(vals)} 项）</text></svg>'
    lo=min([r[k] for r in numeric for k in ('base','patched')]); hi=max([r[k] for r in numeric for k in ('base','patched')]); span=hi-lo or 1
    x=lambda v: 260+390*(v-lo)/span
    h=30+len(numeric)*27
    parts=[f'<h3>{e(title)} · 修改前后</h3><svg viewBox="0 0 700 {h}" role="img" aria-label="修改前后">']
    for i,r in enumerate(numeric):
        y=20+i*27; label=key_label(path,r['key'])+' · '+str(r['field']); tip=f'{label}: {num(r["base"])} → {num(r["patched"])}'
        parts.append(f'<text x="5" y="{y+4}">{e(label)}</text><line class="range" x1="{x(r["base"]):.2f}" x2="{x(r["patched"]):.2f}" y1="{y}" y2="{y}"/><circle cx="{x(r["base"]):.2f}" cy="{y}" r="4" fill="#888"><title>{e(tip)}</title></circle><circle cx="{x(r["patched"]):.2f}" cy="{y}" r="5" fill="#1261a0"><title>{e(tip)}</title></circle>')
    parts.append('</svg><p class="muted">灰色为修改前，蓝色为修改后；悬停查看数值。</p>')
    for r in rows:
        if r not in numeric: parts.append(f'<p>{e(key_label(path,r["key"]))}：{e(r["base"])} → {e(r["patched"])}</p>')
    return ''.join(parts)


def simulation(comp):
    if comp.manifest['skip_sim']: return empty('已跳过模拟；空表不代表没有影响。')
    result=[]
    labels={'island_arrival':'到岛日','daily':'每日进度','timechunk_stats':'属性成长','combat':'战斗','evolution':'进化','first_kill':'首杀','items':'物品','item_first':'物品首次出现','rpm':'每分钟成长'}
    days=(comp.patch.report or {}).get('key_days',[5,10,20,46,104,206,405])
    for name,label in labels.items():
        a=comp.simulation.get('baseline',{}).get(name,[]); b=comp.simulation.get('patched',{}).get(name,[])
        vacuous = comp.simulation.get('vacuous_tables', {}).get(f'sim/{name}.csv')
        if vacuous or (not a and not b):
            result.append(f'<h3>{e(label)}</h3>'+empty(vacuous or '两侧没有数据，当前重建未提供此结果。')); continue
        # Each numerical field gets a paired comparison chart; day or category supplies the horizontal axis.
        fields=sorted({k for r in a+b for k,v in r.items() if k not in ('day','island','sequence','round','stat','type','spawner','item','kind') and isinstance(v,(int,float)) and not isinstance(v,bool)})
        if name == 'daily':
            fields = [k for k in ('active_minutes', 'kills', 'clears', 'fight_time_sum', 'total_playtime') if k in fields]
        if name in ('island_arrival','first_kill','item_first','evolution'): fields=['day'] if any(isinstance(r.get('day'),(int,float)) for r in a+b) else fields
        result.append(f'<h3>{e(label)}</h3>')
        for field in fields[:5]:
            points=[]
            for side, data in enumerate((a,b)):
                for index,r in enumerate(data):
                    value=r.get(field); xv=r.get('day') if field!='day' else r.get('island',r.get('sequence',index))
                    if isinstance(xv,(int,float)) and isinstance(value,(int,float)) and math.isfinite(xv) and math.isfinite(value): points.append((side,xv,value,r))
            if not points: continue
            # Downsample deterministically to bound file size while retaining both ends.
            if len(points)>1600: points=points[::math.ceil(len(points)/1600)]
            xmin=min(p[1] for p in points); xmax=max(p[1] for p in points); ymin=min(p[2] for p in points); ymax=max(p[2] for p in points)
            xp=lambda v: 50+610*(v-xmin)/(xmax-xmin or 1); yp=lambda v: 180-150*(v-ymin)/(ymax-ymin or 1)
            field_label = {'active_minutes':'活跃分钟', 'kills':'击杀数', 'clears':'清除次数', 'fight_time_sum':'战斗时长', 'total_playtime':'累计游戏时间', 'value':'数值', 'total_value':'总数值', 'gpm':'每分钟成长', 'day':'天数', 'level':'等级', 'copies':'数量'}.get(field, field)
            svg=[f'<h4>{e(field_label)}</h4><svg viewBox="0 0 700 220" role="img" aria-label="{e(label)} {e(field)}">']
            if field!='day':
                for d in days:
                    if isinstance(d,(int,float)) and xmin<=d<=xmax: svg.append(f'<line class="keyday" x1="{xp(d):.2f}" x2="{xp(d):.2f}" y1="25" y2="180"><title>关键日 {e(d)}</title></line>')
            if field != 'day':
                series = defaultdict(list)
                for side, xv, value, row in points:
                    identity = tuple(str(row.get(k, '')) for k in ('stat', 'kind', 'type', 'item', 'spawner'))
                    series[(side, identity)].append((xv, value))
                for (side, _), values in sorted(series.items()):
                    if len(values) > 1:
                        coords = ' '.join(f'{xp(x):.2f},{yp(y):.2f}' for x, y in sorted(values))
                        svg.append(f'<polyline points="{coords}" fill="none" stroke="{["#888888","#1261a0"][side]}" stroke-width="1"/>')
            for side,xv,value,row in points:
                svg.append(f'<circle cx="{xp(xv):.2f}" cy="{yp(value):.2f}" r="2.5" fill="{["#888888","#1261a0"][side]}"><title>{e(tables.text(row))}</title></circle>')
            svg.append(f'<text x="50" y="205">{num(xmin)} → {num(xmax)}；纵轴 {num(ymin)} → {num(ymax)}</text></svg>')
            result.append(''.join(svg))
        if not fields: result.append(empty('没有可绘制的数值。'))
    return '<p class="muted">灰色为基线，蓝色为改动后；虚线为关键天。大量点按顺序抽样。</p>'+''.join(result)


def render(comp):
    rows=spawners(comp)
    statics=[(p,r) for p,r in sorted(comp.comparisons.items()) if p.startswith('static/') and r]
    unique={r['id']:r for r in comp.formulas}; counts=Counter(r['status'] for r in unique.values())
    coverage={k:sum(k in r['coverage'] for r in unique.values()) for k in ('Python','C# 重建','C# 原生')}
    missing=sum(r['implementers']==['未建模'] for r in unique.values())
    sim='已跳过模拟' if comp.manifest['skip_sim'] else '模拟已运行：'+tables.text(comp.patch.simulate or {})+'；'+str(comp.manifest.get('simulation_label',''))
    header=f'<header><h1>{e(comp.patch.name)}</h1><p>{e(comp.patch.description)}</p><p>{e(sim)}</p>'+('<strong>无变化</strong>' if not comp.changed else '')+'<p class="muted">两侧使用同一模型比较。model_floor 是背景残差，不是本次改动效果。校准模拟尚不能证明闭环可信度。</p></header>'
    summary=f'<div class="cards"><div>覆盖操作<strong>{len(comp.patch.overrides):,}</strong></div><div>展开叶子<strong>{len(comp.static.report.leaves):,}</strong></div><div>变化的烘焙刷怪点<strong>{len(rows):,}</strong></div></div>'
    med=[]
    for island in sorted({str(r['island']) for r in rows}):
        vals=[]
        for metric in ('hp','damage'):
            v=[pct(r[metric+'0'],r[metric+'1']) for r in rows if str(r['island'])==island and r[metric+'0'] != r[metric+'1']]; v=[x for x in v if x is not None and math.isfinite(x)]
            vals.append(signed(statistics.median(v)) if v else '—')
        med.append(f'<li>岛 {e(island)}：HP {e(vals[0])}；伤害 {e(vals[1])}</li>')
    summary += '<ul>'+''.join(med)+'</ul>' if med else empty('没有刷怪点变化，无法计算各岛中位数。')
    formulas=f'<p>触及 {len(unique):,} 条；状态：{e(dict(sorted(counts.items())))}；覆盖：{e(coverage)}；未建模 {missing:,} 条。</p><p class="muted">依赖连接较粗，会多算；覆盖类别可以重叠，原生覆盖不代表本次运行经过该分支。</p>'
    formulas += '<details><summary>查看规则</summary><ul>'+''.join(f'<li>{e(r["id"])} · {e(r["name"])} · {e(r["status"])} · {e(r["coverage"])}</li>' for r in sorted(unique.values(),key=lambda r:r['id']))+'</ul></details>' if unique else empty('没有规则被触及。')
    warnings='<ul>'+''.join(f'<li>{e(w.get("message", ""))} <span class="muted">{e({k:v for k,v in w.items() if k not in ("code","message")})}</span></li>' for w in sorted(comp.warnings,key=tables.text))+'</ul>' if comp.warnings else empty('本次没有警告。')
    table='<input id="search" type="search" placeholder="搜索刷怪点或岛" aria-label="搜索刷怪点"><div class="tablebox"><table id="spawners"><thead><tr>'+''.join(f'<th><button type="button">{e(h)} ↕</button></th>' for h in ('刷怪点 file_id','岛','烘焙日 前','烘焙日 后','HP 前','HP 后','HP 变化','伤害 前','伤害 后','伤害变化'))+'</tr></thead><tbody>'
    for r in rows:
        hp=pct(r['hp0'],r['hp1']); dmg=pct(r['damage0'],r['damage1'])
        values=[r['id'],r['island'],num(r['day0']),num(r['day1']),num(r['hp0']),num(r['hp1']),signed(hp),num(r['damage0']),num(r['damage1']),signed(dmg)]
        cells=[]
        for i,v in enumerate(values):
            p=hp if i in (5,6) else dmg if i in (8,9) else None
            cls='up' if p is not None and p>0 else 'down' if p is not None and p<0 else ''
            raw=[r['id'],r['island'],r['day0'],r['day1'],r['hp0'],r['hp1'],hp,r['damage0'],r['damage1'],dmg][i]
            cells.append(f'<td class="{cls}" data-sort="{e(raw)}">{e(v)}</td>')
        table+='<tr>'+''.join(cells)+'</tr>'
    table+='</tbody></table></div>'
    body=header+section('改动摘要',summary)+section('刷怪点散点图',scatter(rows,'hp','HP')+scatter(rows,'damage','伤害') if rows else empty('没有变化的刷怪点。'))+section('各岛变化范围',ranges(rows))+section('静态表变化',''.join(static_chart(p,r) for p,r in statics) if statics else empty('静态表没有变化。'))+section('模拟',simulation(comp))+section('依赖图触及的规则',formulas)+section('警告',warnings)+section('刷怪点明细',table if rows else empty('没有变化的刷怪点。'))
    return '<!doctype html><html lang="zh-CN"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>'+e(comp.patch.name)+' · 数值对比</title><style>'+CSS+'</style></head><body><main>'+body+'</main><script>'+JS+'</script></body></html>\n'


CSS = '''
:root{color-scheme:light dark;--bg:#f5f7fa;--panel:#fff;--ink:#182432;--muted:#526273;--line:#c6d0dc;--up:#b63627;--down:#08715a} @media(prefers-color-scheme:dark){:root{--bg:#111820;--panel:#202b36;--ink:#eef4fa;--muted:#b8c7d5;--line:#536577;--up:#ffb0a2;--down:#7de1c0}} *{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--ink);font:16px/1.6 system-ui,sans-serif}main{max-width:1100px;margin:auto;padding:18px}header,section{background:var(--panel);padding:20px;margin-bottom:16px;border-radius:10px}h1,h2,h3,h4{line-height:1.3}h1{overflow-wrap:anywhere}h2{border-bottom:1px solid var(--line);padding-bottom:8px}.muted{color:var(--muted);font-size:.9em}.cards{display:flex;flex-wrap:wrap;gap:10px}.cards div{flex:1 1 150px;border:1px solid var(--line);padding:12px;border-radius:8px}.cards strong{display:block;font-size:1.5em}svg{display:block;width:100%;height:auto;overflow:visible}svg text{fill:var(--ink);font-size:12px}svg .count{fill:white;text-anchor:middle;font-size:9px;pointer-events:none}svg rect{fill:#1261a0}svg .zero{stroke:var(--muted);stroke-dasharray:5 4}svg .keyday{stroke:var(--muted);stroke-dasharray:2 5}svg .range{stroke:var(--muted);stroke-width:3}.tablebox{overflow-x:auto;max-width:100%}table{border-collapse:collapse;white-space:nowrap}th,td{text-align:left;border-bottom:1px solid var(--line);padding:7px}th button{font:inherit;font-weight:bold;color:inherit;background:none;border:0;cursor:pointer}.up{color:var(--up);font-weight:bold}.down{color:var(--down);font-weight:bold}input{max-width:100%;padding:8px;font:inherit}li{overflow-wrap:anywhere}.legend span{display:inline-block;margin-right:12px}.legend i{display:inline-block;width:10px;height:10px;margin-right:4px;border-radius:50%}@media(max-width:500px){main{padding:8px}header,section{padding:12px}svg text{font-size:14px}}
'''
JS = '''
const table=document.getElementById('spawners'), search=document.getElementById('search');
if(table){search.addEventListener('input',()=>{const q=search.value.toLocaleLowerCase();for(const r of table.tBodies[0].rows)r.hidden=!r.textContent.toLocaleLowerCase().includes(q)});table.querySelectorAll('th button').forEach((b,i)=>b.addEventListener('click',()=>{const body=table.tBodies[0],rows=Array.from(body.rows),dir=b.dataset.dir==='up'?-1:1;b.dataset.dir=dir===1?'up':'down';rows.sort((a,c)=>{const x=a.cells[i].dataset.sort,y=c.cells[i].dataset.sort,n=Number(x),m=Number(y);return dir*(x!==''&&y!==''&&Number.isFinite(n)&&Number.isFinite(m)?n-m:x.localeCompare(y,undefined,{numeric:true}))});rows.forEach(r=>body.appendChild(r))}))}
'''
