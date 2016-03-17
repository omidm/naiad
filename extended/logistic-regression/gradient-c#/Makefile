
NAIAD_ROOT = ../..
SOURCE = Gradient.cs

default: release


TARGET_R         = Release/Gradient.exe
MONO_BUILD_R     =
BUILD_DIR_R      = Release/

release: $(TARGET_R)

$(TARGET_R): $(SOURCE)
	mkdir -p $(BUILD_DIR_R) 
	dmcs $(MONO_BUILD_R) -out:$(TARGET_R) $(SOURCE)

run: release
	cd $(BUILD_DIR_R); mono Gradient.exe 10 20 1 


TARGET_D         = Debug/Gradient.exe
MONO_BUILD_D     = -debug
BUILD_DIR_D      = Debug/

debug: $(TARGET_D)

$(TARGET_D): $(SOURCE)
	mkdir -p $(BUILD_DIR_D) 
	dmcs $(MONO_BUILD_D) -out:$(TARGET_D) $(SOURCE)

debug-run: debug
	cd $(BUILD_DIR_D); mono --debug Gradient.exe 10 20 1 


clean:
	\rm -rf Release Debug
