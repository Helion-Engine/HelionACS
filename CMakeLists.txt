cmake_minimum_required(VERSION 3.16)

set(CMAKE_EXPORT_COMPILE_COMMANDS ON)
set(CMAKE_CXX_STANDARD 20)

project(HelionACS-native LANGUAGES CXX)

add_library(HelionACS-native SHARED
    src/cxx/HelionACS.cpp
    src/cxx/ACSVM/ACSVM/Action.cpp
    src/cxx/ACSVM/ACSVM/Array.cpp
    src/cxx/ACSVM/ACSVM/CallFunc.cpp
    src/cxx/ACSVM/ACSVM/CodeData.cpp
    src/cxx/ACSVM/ACSVM/Environment.cpp
    src/cxx/ACSVM/ACSVM/Error.cpp
    src/cxx/ACSVM/ACSVM/Function.cpp
    src/cxx/ACSVM/ACSVM/Init.cpp
    src/cxx/ACSVM/ACSVM/Jump.cpp
    src/cxx/ACSVM/ACSVM/Module.cpp
    src/cxx/ACSVM/ACSVM/ModuleACS0.cpp
    src/cxx/ACSVM/ACSVM/ModuleACSE.cpp
    src/cxx/ACSVM/ACSVM/PrintBuf.cpp
    src/cxx/ACSVM/ACSVM/Scope.cpp
    src/cxx/ACSVM/ACSVM/Script.cpp
    src/cxx/ACSVM/ACSVM/Serial.cpp
    src/cxx/ACSVM/ACSVM/SerialSTD.cpp
    src/cxx/ACSVM/ACSVM/String.cpp
    src/cxx/ACSVM/ACSVM/Thread.cpp
    src/cxx/ACSVM/ACSVM/ThreadExec.cpp
    src/cxx/ACSVM/ACSVM/Tracer.cpp
)

target_compile_definitions(HelionACS-native PRIVATE HELIONACS_BUILD)

target_include_directories(HelionACS-native PUBLIC
    ${CMAKE_CURRENT_SOURCE_DIR}
)
if (WIN32)
    set_target_properties(HelionACS-native PROPERTIES
        PREFIX ""
    )
endif()
