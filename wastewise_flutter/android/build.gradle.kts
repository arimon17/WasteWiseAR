allprojects {
    repositories {
        google()
        mavenCentral()
        maven { url = uri("https://jitpack.io") }
        flatDir {
            dirs("${project(":unityLibrary").projectDir}/libs")
        }
    }
}

subprojects {
    buildscript {
        repositories {
            google()
            mavenCentral()
        }
    }

    configurations.all {
        resolutionStrategy.eachDependency {
            // jcenter() is dead; artifacts resolve from google()/mavenCentral().
        }
    }
}

val newBuildDir: Directory =
    rootProject.layout.buildDirectory
        .dir("../../build")
        .get()
rootProject.layout.buildDirectory.value(newBuildDir)

subprojects {
    val newSubprojectBuildDir: Directory = newBuildDir.dir(project.name)
    project.layout.buildDirectory.value(newSubprojectBuildDir)
}
subprojects {
    project.evaluationDependsOn(":app")
}

tasks.register<Delete>("clean") {
    delete(rootProject.layout.buildDirectory)
}

gradle.projectsEvaluated {
    val unityLibrary = rootProject.project(":unityLibrary")
    unityLibrary.tasks.matching { it.name == "buildIl2Cpp" }.configureEach {
        val hasPrebuiltIl2Cpp = listOf("arm64-v8a").all { abi ->
            java.io.File(unityLibrary.projectDir, "src/main/jniLibs/$abi/libil2cpp.so").exists()
        }
        enabled = !hasPrebuiltIl2Cpp

        if (hasPrebuiltIl2Cpp) {
            logger.lifecycle("[WasteWise] buildIl2Cpp dilewati: libil2cpp.so arm64-v8a prebuilt tersedia.")
        }
    }
}
