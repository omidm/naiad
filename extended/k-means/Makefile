
NAIAD_ROOT = ../..
SOURCE = KMeans.cs

default: release


TARGET_R         = Release/KMeans.exe
MONO_BUILD_R     =
BUILD_DIR_R      = Release/
NAIAD_LIB_R      = Release/Microsoft.Research.Naiad.dll
LINDI_LIB_R      = Release/Microsoft.Research.Naiad.Lindi.dll
GLINQ_LIB_R      = Release/Microsoft.Research.Naiad.GraphLINQ.dll
NAIAD_LIB_SRC_R  = $(NAIAD_ROOT)/Naiad/bin/Release/Microsoft.Research.Naiad.dll
LINDI_LIB_SRC_R  = $(NAIAD_ROOT)/Examples/bin/Release/Microsoft.Research.Naiad.Lindi.dll
GLINQ_LIB_SRC_R  = $(NAIAD_ROOT)/Examples/bin/Release/Microsoft.Research.Naiad.GraphLINQ.dll


$(BUILD_DIR_R)%.dll:
	cd $(NAIAD_ROOT); sh build_mono_new.sh $(MONO_BUILD_R); cd -;
	mkdir -p $(BUILD_DIR_R) 
	cp $(NAIAD_LIB_SRC_R) $(NAIAD_LIB_R) 
	cp $(LINDI_LIB_SRC_R) $(LINDI_LIB_R) 
	cp $(GLINQ_LIB_SRC_R) $(GLINQ_LIB_R) 


release: $(TARGET_R)

$(TARGET_R): $(SOURCE) $(NAIAD_LIB_R) $(LINDI_LIB_R) $(GLINQ_LIB_R)
	dmcs $(MONO_BUILD_R) -out:$(TARGET_R) $(SOURCE) /reference:$(NAIAD_LIB_R) /reference:$(LINDI_LIB_R) /reference:$(GLINQ_LIB_R)

run: release
	cd $(BUILD_DIR_R); mono KMeans.exe -t 2 10 2 15 4 1 1 0

run-spin: release
	cd $(BUILD_DIR_R); mono KMeans.exe -t 2 10 2 15 4 1 1 5000

run-dist: release
	cd $(BUILD_DIR_R); xterm -hold -e mono KMeans.exe -n 2 --local -p 0 -t 2 --inlineserializer 10 2 15 8 1 2 0 &
	sleep 1
	cd $(BUILD_DIR_R); xterm -hold -e mono KMeans.exe -n 2 --local -p 1 -t 2 --inlineserializer 10 2 15 8 1 2 0



TARGET_D         = Debug/KMeans.exe
MONO_BUILD_D     = -debug -d:DEBUG
BUILD_DIR_D      = Debug/
NAIAD_LIB_D      = Debug/Microsoft.Research.Naiad.dll
LINDI_LIB_D      = Debug/Microsoft.Research.Naiad.Lindi.dll
GLINQ_LIB_D      = Debug/Microsoft.Research.Naiad.GraphLINQ.dll
NAIAD_LIB_SRC_D  = $(NAIAD_ROOT)/Naiad/bin/Debug/Microsoft.Research.Naiad.dll
LINDI_LIB_SRC_D  = $(NAIAD_ROOT)/Examples/bin/Debug/Microsoft.Research.Naiad.Lindi.dll
GLINQ_LIB_SRC_D  = $(NAIAD_ROOT)/Examples/bin/Debug/Microsoft.Research.Naiad.GraphLINQ.dll


$(BUILD_DIR_D)%.dll:
	cd $(NAIAD_ROOT); sh build_mono_new.sh $(MONO_BUILD_D); cd -;
	mkdir -p $(BUILD_DIR_D) 
	cp $(NAIAD_LIB_SRC_D) $(NAIAD_LIB_D) 
	cp $(LINDI_LIB_SRC_D) $(LINDI_LIB_D) 
	cp $(GLINQ_LIB_SRC_D) $(GLINQ_LIB_D) 


debug: $(TARGET_D)

$(TARGET_D): $(SOURCE) $(NAIAD_LIB_D) $(LINDI_LIB_D) $(GLINQ_LIB_D)
	dmcs $(MONO_BUILD_D) -out:$(TARGET_D) $(SOURCE) /reference:$(NAIAD_LIB_D) /reference:$(LINDI_LIB_D) /reference:$(GLINQ_LIB_D)

debug-run: debug
	export MONO_TRACE_LISTENER=Console.Error; cd $(BUILD_DIR_D); mono --debug KMeans.exe -t 2 10 2 15 4 1 1 0

debug-run-spin: debug
	cd $(BUILD_DIR_D); mono --debug KMeans.exe -t 2 10 2 15 4 1 1 5000

debug-run-dist: debug
	cd $(BUILD_DIR_D); xterm -hold -e mono --debug KMeans.exe -n 2 --local -p 0 -t 2 --inlineserializer 10 2 15 8 1 2 0 &
	sleep 1
	cd $(BUILD_DIR_D); xterm -hold -e mono --debug KMeans.exe -n 2 --local -p 1 -t 2 --inlineserializer 10 2 15 8 1 2 0


clean:
	\rm -rf Release Debug
