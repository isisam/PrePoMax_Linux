#include "vtkmax.h"
#include <stddef.h>
_Static_assert(sizeof(vm_pick_result) == 56, "pick ABI size");
_Static_assert(offsetof(vm_pick_result, sequence) == 48, "pick ABI offset");
int main(void) { return vm_abi_version() == 1 ? 0 : 1; }
