import sys,unittest,tempfile,struct,hashlib
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'tools'))
import prepare_game as pg
class ImportTests(unittest.TestCase):
 def test_folder_and_wrong_revision(self):
  with tempfile.TemporaryDirectory() as t:
   root=Path(t);source=root/'source';source.mkdir();(source/'GAMEDATA').mkdir();(source/'DEFAULT.XBE').write_bytes(b'synthetic');(source/'GAMEDATA/english.xbr').write_bytes(b'archive');(source/'english.txt').write_bytes(b'text');out=root/'project';out.mkdir()
   old=pg.EXPECTED
   try:
    with self.assertRaises(ValueError):pg.prepare(source,out)
    self.assertFalse((out/'assets').exists())
    pg.EXPECTED=hashlib.sha1(b'synthetic').hexdigest();pg.prepare(source,out)
    self.assertEqual((out/'assets/default.xbe').read_bytes(),b'synthetic')
    self.assertEqual((source/'DEFAULT.XBE').read_bytes(),b'synthetic')
   finally:pg.EXPECTED=old
 def test_xiso_and_bounds(self):
  with tempfile.TemporaryDirectory() as t:
   path=Path(t)/'test.iso';data=bytearray(40*2048);v=32*2048;data[v:v+20]=pg.MAGIC;data[v+0x7ec:v+2048]=pg.MAGIC;struct.pack_into('<II',data,v+20,33,40)
   entry=33*2048;struct.pack_into('<HHIIBB',data,entry,0,0,34,4,0,11);data[entry+14:entry+25]=b'default.xbe';data[34*2048:34*2048+4]=b'XBEH';path.write_bytes(data)
   disc=pg.Xiso(path)
   try:
    self.assertEqual(disc.load('DEFAULT.XBE'),b'XBEH')
    with self.assertRaises(ValueError):disc.read(len(data)-1,10)
    with self.assertRaises(ValueError):disc.load('missing')
   finally:disc.close()
 def test_bad_iso(self):
  with tempfile.TemporaryDirectory() as t:
   p=Path(t)/'broken.iso';p.write_bytes(b'bad')
   with self.assertRaises(ValueError):pg.Xiso(p)
