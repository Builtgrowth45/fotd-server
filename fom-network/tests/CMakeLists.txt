# GoogleTest requires at least C++11
set(CMAKE_CXX_STANDARD 11)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

include(FetchContent)
FetchContent_Declare(
    googletest
    URL https://github.com/google/googletest/archive/6a7ed316a5cdc07b6d26362c90770787513822d4.zip
)
# For Windows: Prevent overriding the parent project's compiler/linker settings
set(gtest_force_shared_crt ON CACHE BOOL "" FORCE)

# Suppress warnings from GoogleTest's old CMakeLists.txt
set(CMAKE_WARN_DEPRECATED OFF CACHE BOOL "" FORCE)
set(CMAKE_SUPPRESS_DEVELOPER_WARNINGS ON CACHE BOOL "" FORCE)

# The pinned GoogleTest declares cmake_minimum_required(VERSION 2.8.8). CMake 4
# removed compatibility below 3.5 and refuses to configure it at all, so the
# floor is raised for GoogleTest only and put back immediately afterwards.
if(CMAKE_VERSION VERSION_GREATER_EQUAL 4.0)
    set(_fom_saved_policy_minimum "${CMAKE_POLICY_VERSION_MINIMUM}")
    set(CMAKE_POLICY_VERSION_MINIMUM 3.5)
endif()

FetchContent_MakeAvailable(googletest)

if(CMAKE_VERSION VERSION_GREATER_EQUAL 4.0)
    set(CMAKE_POLICY_VERSION_MINIMUM "${_fom_saved_policy_minimum}")
    unset(_fom_saved_policy_minimum)
endif()

set(CMAKE_WARN_DEPRECATED ON CACHE BOOL "" FORCE)
set(CMAKE_SUPPRESS_DEVELOPER_WARNINGS OFF CACHE BOOL "" FORCE)

# Create the test executable FIRST
add_executable(FOMNetworkTests ${PROJECT_SOURCE_DIR}/${FOMNETWORK_TEST})

target_include_directories(FOMNetworkTests PRIVATE
    ${PROJECT_SOURCE_DIR}/include
    ${PROJECT_SOURCE_DIR}/src
)

target_link_libraries(FOMNetworkTests
    PRIVATE
    FOMNetwork_obj
    RakNet
    gtest_main
)

# Now add the POST_BUILD copy for Windows
if (MSVC)
    set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>DLL")

    # Disable Microsoft Code Analysis for GoogleTest targets
    set_target_properties(gtest PROPERTIES VS_GLOBAL_EnableMicrosoftCodeAnalysis false)
    set_target_properties(gtest_main PROPERTIES VS_GLOBAL_EnableMicrosoftCodeAnalysis false)

    # Copy FOMNetwork.dll to the test executable directory
    add_custom_command(TARGET FOMNetworkTests POST_BUILD
        COMMAND ${CMAKE_COMMAND} -E copy_if_different
            $<TARGET_FILE:FOMNetwork>
            $<TARGET_FILE_DIR:FOMNetworkTests>
    )
endif()

# Discover the GoogleTest tests
include(GoogleTest)
gtest_discover_tests(FOMNetworkTests
    EXTRA_ARGS --gtest_color=yes
)
