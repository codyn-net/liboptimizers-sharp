

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = $(CMCS)
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
ASSEMBLY_COMPILER_COMMAND = $(CMCS)
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
	Optimization.Optimizers.ADPSO/ADPSO.cs \
	Optimization.Optimizers.ADPSO/Particle.cs \
	Optimization.Optimizers.ADPSO/Settings.cs \
	Optimization.Optimizers.GA/GA.cs \
	Optimization.Optimizers.GA/Settings.cs \
	Optimization.Optimizers.GCPSO/GCPSO.cs \
	Optimization.Optimizers.GCPSO/Settings.cs \
	Optimization.Optimizers.PMPSO/Fusion.cs \
	Optimization.Optimizers.PMPSO/MutationSet.cs \
	Optimization.Optimizers.PMPSO/Neighborhood.cs \
	Optimization.Optimizers.PMPSO/Particle.cs \
	Optimization.Optimizers.PMPSO/PMPSO.cs \
	Optimization.Optimizers.PMPSO/Settings.cs \
	Optimization.Optimizers.PMPSO/State.cs \
	Optimization.Optimizers.PSO/Particle.cs \
	Optimization.Optimizers.PSO/PSO.cs \
	Optimization.Optimizers.PSO/State.cs \
	Optimization.Optimizers.PSO/IPSOExtension.cs \
	Optimization.Optimizers.PSO/Settings.cs \
	Optimization.Optimizers.Systematic/Range.cs \
	Optimization.Optimizers.Systematic/Systematic.cs \
	Optimization.Optimizers.Systematic/Settings.cs \
	Optimization.Optimizers.SPSA/SPSA.cs \
	Optimization.Optimizers.SPSA/Settings.cs \
	Optimization.Optimizers.SPSA/Solution.cs \
	Optimization.Optimizers.SPSA/Algorithm.cs \
	Optimization.Optimizers.Extensions.RegPSO/RegPSO.cs \
	Optimization.Optimizers.Extensions.RegPSO/Settings.cs \
	Optimization.Optimizers.Extensions.LPSO/ConstraintMatrix.cs \
	Optimization.Optimizers.Extensions.LPSO/Linear.cs \
	Optimization.Optimizers.Extensions.LPSO/LPSO.cs \
	Optimization.Optimizers.Extensions.LPSO/Settings.cs \
	Optimization.Optimizers.Extensions.StagePSO/StagePSO.cs \
	Optimization.Optimizers.Extensions.StagePSO/Stage.cs \
	Optimization.Optimizers.Extensions.DPSO/DPSO.cs \
	Optimization.Optimizers.Extensions.DPSO/Settings.cs

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	optimizers-sharp.pc.in 

REFERENCES =  \
	System \
	System.Xml \
	System.Data \
	$(OPTIMIZATION_SHARP_LIBS)

DLL_REFERENCES = 

CLEANFILES = $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

OPTIMIZERS_SHARP_PC = $(BUILD_DIR)/optimizers-sharp-@LIBOPTIMIZERS_SHARP_API_VERSION@.pc
OPTIMIZERS_SHARP_API_PC = optimizers-sharp-@LIBOPTIMIZERS_SHARP_API_VERSION@.pc

pc_files = $(OPTIMIZERS_SHARP_API_PC)

include $(top_srcdir)/Makefile.include

$(eval $(call emit-deploy-wrapper,OPTIMIZERS_SHARP_PC,$(OPTIMIZERS_SHARP_API_PC)))

$(OPTIMIZERS_SHARP_API_PC): optimizers-sharp.pc
	cp $< $@

$(eval $(call emit_resgen_targets))
$(build_xamlg_list): %.xaml.g.cs: %.xaml
	xamlg '$<'

$(ASSEMBLY) $(ASSEMBLY_MDB): $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES) $(build_xamlg_list) $(build_satellite_assembly_list)
	mkdir -p $(shell dirname $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$(ASSEMBLY) -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)

.NOTPARALLEL:
