"""Independent limiting cases and MPC topology checks for the CLT benchmark."""
import os
import tempfile
import unittest

from Composite_Layup_Utils import LAMINA, blocks, clt, periodic_deck, run_solver


class CompositeBenchmarkTests(unittest.TestCase):
    def test_unidirectional_compliance_and_quarter_turn(self):
        for angle in (0,90):
            result = clt([(angle,t) for t in (.1,.2,.3,.4)])
            e1,e2,_,nu12 = LAMINA[:4]
            self.assertAlmostEqual(result["Ex"], e1 if angle == 0 else e2)
            self.assertAlmostEqual(result["Ey"], e2 if angle == 0 else e1)
            self.assertAlmostEqual(result["nu_xy"], nu12 if angle == 0 else nu12*e2/e1)

    def test_exact_thickness_integration_and_scale_invariance(self):
        # 0/90 cross-ply with unequal ply thicknesses has a direct 2x2 A formula.
        e1,e2,_,nu = LAMINA[:4]
        d = 1-nu*nu*e2/e1
        a11,a22,a12 = (.2*e1+.8*e2)/d, (.2*e2+.8*e1)/d, nu*e2/d
        data = [(0,.1),(90,.4),(90,.4),(0,.1)]
        target = clt(data)
        self.assertAlmostEqual(target["Ex"], a11-a12*a12/a22)
        self.assertAlmostEqual(target["Ey"], a22-a12*a12/a11)
        self.assertAlmostEqual(target["nu_xy"], a12/a22)
        scaled = clt([(a,17*t) for a,t in data])
        for key in ("Ex","Ey","nu_xy"):
            self.assertAlmostEqual(target[key],scaled[key])

    def test_balanced_pairs_cancel_extension_shear_coupling(self):
        target = clt([(45,.125),(-45,.125),(-45,.125),(45,.125)])
        self.assertAlmostEqual(target["A"][0][2],0.)
        self.assertAlmostEqual(target["A"][1][2],0.)
        self.assertAlmostEqual(target["Ex"],target["Ey"])

    def test_periodic_pairs_have_no_duplicate_dependent_dof_or_corner_loop(self):
        nx,ny,nz = 4,2,9
        with tempfile.TemporaryDirectory() as directory:
            path = os.path.join(directory,"cell.inp")
            with open(path,"w") as stream:
                stream.write("** topology fixture\n")
            rx,ry = periodic_deck(path,nx,ny,[.125*k for k in range(nz)])
            with open(path) as stream:
                cards = blocks(stream.read())
        rows = next(rows for key,_,rows in cards if key == "*EQUATION")
        equations = [[float(s) for s in row.split(",")] for row in rows[1::2]]
        dependencies = [(int(e[0]),int(e[1])) for e in equations]
        self.assertEqual(len(dependencies),3*nz*(nx+ny+1))
        self.assertEqual(len(set(dependencies)),len(dependencies))
        self.assertNotIn((1,1),dependencies)
        self.assertTrue(all(n not in (rx,ry) for n,_ in dependencies))
        # A dependent node on X1 can refer to Y1; those Y1 dependencies must
        # refer back to Y0, never form a cycle through a duplicate corner.
        edges = {d: (int(e[3]),int(e[4])) for d,e in zip(dependencies,equations)}
        for start in edges:
            visited = set()
            current = start
            while current in edges:
                self.assertNotIn(current,visited)
                visited.add(current)
                current = edges[current]
        boundaries = next(rows for key,_,rows in cards if key == "*BOUNDARY")
        self.assertEqual(len(boundaries),4)  # origin, RX.2/3, RY.3, axial macro strain only

    def test_stock_fatczm_guard_precedes_launch(self):
        with tempfile.TemporaryDirectory() as directory:
            path = os.path.join(directory,"fat.inp")
            with open(path,"w") as stream:
                stream.write("*MATERIAL,NAME=UserFatCzm_Test\n*USER MATERIAL,CONSTANTS=1\n1\n")
            with self.assertRaisesRegex(Exception,"FATCZM deck requires ccx_fat.exe"):
                run_solver("nonexistent_stock_ccx.exe",path,"must_not_launch")
            self.assertFalse(os.path.exists(os.path.join(directory,"must_not_launch")))


if __name__ == "__main__":
    unittest.main()
