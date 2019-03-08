cmake_minimum_required(VERSION 3.2)

project(com)
set(SOURCES
        COMIntegration.cpp
        BStrHolder.h
        ComPtr.h
        dte80a.tlh
        )
add_executable(com ${SOURCES})