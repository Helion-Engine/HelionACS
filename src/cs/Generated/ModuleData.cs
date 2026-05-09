namespace HelionACS.Interop
{
    public unsafe partial struct ModuleData
    {
        [NativeTypeName("std::size_t")]
        public nuint length;

        [NativeTypeName("ACSVM::Byte *")]
        public byte* data;
    }
}
