

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize- -debug "-define:DEBUG"
ASSEMBLY = bin/Debug/Optimization.Optimizers.dll
ASSEMBLY_MDB = $(ASSEMBLY).mdb
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = bin/Debug

OPTIMIZERS_SHARP_DLL_MDB_SOURCE=bin/Debug/Optimization.Optimizers.dll.mdb
OPTIMIZERS_SHARP_DLL_MDB=$(BUILD_DIR)/Optimization.Optimizers.dll.mdb

endif

if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -optimize-
ASSEMBLY = bin/Release/Optimization.Optimizers.dll
ASSEMBLY_MDB = 
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = bin/Release

OPTIMIZERS_SHARP_DLL_MDB=

endif

AL=al2
SATELLITE_ASSEMBLY_NAME=$(notdir $(basename $(ASSEMBLY))).resources.dll

PROGRAMFILES = \
	$(OPTIMIZERS_SHARP_DLL_MDB)  

LINUX_PKGCONFIG = \
	$(OPTIMIZERS_SHARP_PC)  


RESGEN=resgen2
	
all: $(ASSEMBLY) $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

FILES = \
	Optimization.Optimizers/AssemblyInfo.cs \
	Optimization.Optimizers.PSO/Particle.cs \
	Optimization.Optimizers.PSO/PSO.cs \
	Optimization.Optimizers.PSO/Settings.cs \
	Optimization.Optimizers.Systematic/Range.cs \
	Optimization.Optimizers.Systematic/Systematic.cs \
	Optimization.Optimizers.SPSA/SPSA.cs \
	Optimization.Optimizers.SPSA/Settings.cs \
	Optimization.Optimizers.SPSA/Solution.cs

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	optimizers-sharp.pc.in 

REFERENCES =  \
	System \
	System.Xml \
	$(OPTIMIZATION_SHARP_LIBS)

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

include $(top_srcdir)/Makefile.include

OPTIMIZERS_SHARP_PC = $(BUILD_DIR)/optimizers-sharp.pc

$(eval $(call emit-deploy-wrapper,OPTIMIZERS_SHARP_PC,optimizers-sharp.pc))


$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY) $(ASSEMBLY_MDB): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)
