#PrePoMax Python Script
#The PrePoMax object is available as 'pmx'
# Helper run by Run_Composite_Layup_Tests.py after the golden test, with -f M2a_layups.pmx.
import os
import sys
sys.path.insert(0, os.path.dirname(__file__))
from Composite_Layup_Utils import check, ply_cards
work = pmx.work_directory
check(len(pmx.model.composite_layups.get_names()) == 8, "layup .pmx round trip")
check([pmx.model.composite_layups.get(n).get_plies()[0].angle_deg
       for n in pmx.model.composite_layups.get_names()] == [45, 90, -45, 0, 0, -45, 90, 45], "stored order/angles")
out = os.path.join(work, "M2a_reloaded.inp")
pmx.export_to_calculix(out)
with open(out) as stream:
    actual = ply_cards(stream.read())
with open(os.path.join(work, "M2a_golden.inp")) as stream:
    check(actual == ply_cards(stream.read()), "round-trip expanded cards")
check(not any(n.startswith("LU_") for n in pmx.model.element_sets.get_names()), "transient sets in saved model")
pmx.save(os.path.join(work, "M2a_resaved.pmx"))
print("PASS fresh-process typed .pmx reload, ordered plies and identical expanded cards")
print("TEST PASSED")
