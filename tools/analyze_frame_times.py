"""Summarize bounded diagnostic windows; never mislabel window p99 as whole-run p99."""
import argparse,json,re
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('log',type=Path);p.add_argument('--output',type=Path);a=p.parse_args()
pattern=re.compile(r'frame-times: samples (\d+) p50 ([\d.]+) p95 ([\d.]+) p99 ([\d.]+) max ([\d.]+) ms \| over50 (\d+) over100 (\d+) \| pipeline ([\d.]+) ms \| privateMB (-?[\d.]+)')
rows=[]
for match in pattern.finditer(a.log.read_text(errors='replace')):
 v=list(map(float,match.groups()));rows.append(dict(zip(['samples','p50_ms','p95_ms','p99_ms','max_ms','over50','over100','pipeline_ms','private_MB'],v)))
if not rows:p.error('No per-frame diagnostic windows in this log; enable diagnostic in launcher for a later session.')
mem=[r['private_MB'] for r in rows if r['private_MB']>=0]
out={'log':str(a.log),'windows':len(rows),'sampled_frames':int(sum(r['samples'] for r in rows)), 'worst_frame_ms':max(r['max_ms'] for r in rows),'worst_window_p99_ms':max(r['p99_ms'] for r in rows),'frames_over_50ms':int(sum(r['over50'] for r in rows)),'frames_over_100ms':int(sum(r['over100'] for r in rows)),'private_memory_first_last_MB':[mem[0],mem[-1]] if mem else None,'note':'Present-to-present host intervals, not GPU timestamps; memory growth alone does not establish a leak.','worst_windows':sorted(rows,key=lambda r:r['max_ms'],reverse=True)[:10]}
text=json.dumps(out,indent=2)
if a.output:a.output.write_text(text,encoding='utf-8')
print(text)
