namespace HelionACS.Interop
{
    public unsafe partial struct CSThreadInfo
    {
        public void* data;

        [NativeTypeName("FreeCSThreadInfoData")]
        public delegate* unmanaged[Cdecl]<void*, void> freeCallback;
    }
}
