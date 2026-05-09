namespace HelionACS.Interop
{
    public unsafe partial struct CallFuncThreadData
    {
        [NativeTypeName("const char *")]
        public sbyte* printBufData;

        [NativeTypeName("std::size_t")]
        public nuint printBufSize;

        public void* threadInfoData;

        public void* context;
    }
}
