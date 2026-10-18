import ctypes as c
class PointerInjector:
    def __init__(self,display):
        self.x=c.CDLL('libX11.so.6');self.t=c.CDLL('libXtst.so.6')
        self.x.XOpenDisplay.argtypes=[c.c_char_p];self.x.XOpenDisplay.restype=c.c_void_p
        self.x.XFlush.argtypes=[c.c_void_p];self.x.XCloseDisplay.argtypes=[c.c_void_p]
        self.t.XTestFakeButtonEvent.argtypes=[c.c_void_p,c.c_uint,c.c_int,c.c_ulong]
        self.display=self.x.XOpenDisplay(display.encode());assert self.display
    def click(self,button):
        assert self.t.XTestFakeButtonEvent(self.display,button,1,0)
        assert self.t.XTestFakeButtonEvent(self.display,button,0,0)
        self.x.XFlush(self.display)
    def close(self):self.x.XCloseDisplay(self.display)
