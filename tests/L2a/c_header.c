#include "vtkmax.h"
#include <stddef.h>
_Static_assert(sizeof(vm_pick_result) == 64, "pick ABI size");
_Static_assert(offsetof(vm_pick_result, sequence) == 48, "pick ABI offset");
_Static_assert(offsetof(vm_pick_result, actor_id) == 56, "actor ABI offset");
int main(void) { return vm_abi_version() == 3 ? 0 : 1; }
