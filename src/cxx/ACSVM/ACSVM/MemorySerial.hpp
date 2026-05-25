#ifndef ACSVM__MemorySerial_H__
#define ACSVM__MemorySerial_H__

#include "Serial.hpp"
#include "Error.hpp"

namespace ACSVM
{
    class MemorySerial : public Serial
    {
    public:
        MemorySerial(char* buffer, size_t bufferSize)
            : buffer(buffer), bufferSize(bufferSize), pos(0),
            failed(false)
        {
        }

        bool hasFailed() const { return failed; }

        void read(char* out, size_t count) override
        {
            if (pos + count > bufferSize)
                throw ACSVM::SerialError("Read past buffer length");

            memcpy(out, buffer + pos, count);
            pos += count;
        }

        void write(char const* data, size_t count) override
        {
            if (failed)
                return;

            if (pos + count > bufferSize)
            {
                failed = true;
                pos += count;
                return;
            }

            memcpy(buffer + pos, data, count);
            pos += count;
        }

        size_t position() const { return pos; }

    private:
        char* buffer;
        size_t bufferSize;
        size_t pos;

        bool failed;
    };

}

#endif
