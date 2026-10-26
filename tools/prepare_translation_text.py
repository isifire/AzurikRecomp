import json
from pathlib import Path
root=Path(__file__).resolve().parents[1]
rows=json.loads((root/'translation/es/textos.es-ES.json').read_text(encoding='utf-8'))['plain_text']
original=(root/'assets/english.txt').read_text(encoding='utf-16')
for row in rows:
 if row['source'] not in original:raise ValueError('Unexpected english.txt')
 original=original.replace(row['source'],row['text'])
(root/'translation/es/english.txt').write_bytes(('\ufeff'+original.replace('\r\n','\n').replace('\n','\r\n')).encode('utf-16-le'))
