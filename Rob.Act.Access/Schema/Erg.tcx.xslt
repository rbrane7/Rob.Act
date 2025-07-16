<?xml version="1.0" encoding="utf-8"?>
<x:stylesheet version="1.0" xmlns:x="http://www.w3.org/1999/XSL/Transform" xmlns:m="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="m tcx #default"
			  xmlns:tcx="http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2" xmlns="http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2"
			  >
  <x:output method="xml" indent="yes" omit-xml-declaration="yes"/>
  <x:param name="Refine"/>
  <x:param name="Detail"/>
  <x:param name="Action"/>
  <x:param name="Drag"/>
	<x:param name="Temp"/>
	<x:param name="Pres"/>
	<x:param name="Humi"/>

	<x:template match="@*|node()">
	<x:copy>
	  <x:apply-templates select="@*|node()"/>
	</x:copy>
  </x:template>

  <x:template match="tcx:Track"/>
  <x:template match="tcx:Creator"/>
  <x:template match="tcx:Author"/>

  <x:template match="tcx:Drag"><x:copy><x:value-of select="$Drag"/></x:copy></x:template>
  <x:template match="tcx:Action"><x:copy><x:value-of select="$Action"/></x:copy></x:template>
  <x:template match="tcx:Refine"><x:copy><x:value-of select="$Refine"/></x:copy></x:template>
  <x:template match="tcx:Detail"><x:copy><x:value-of select="$Detail"/></x:copy></x:template>
  <x:template match="tcx:Temp"><x:copy><x:value-of select="$Temp"/></x:copy></x:template>
  <x:template match="tcx:Pres"><x:copy><x:value-of select="$Pres"/></x:copy></x:template>
  <x:template match="tcx:Humi"><x:copy><x:value-of select="$Humi"/></x:copy></x:template>

  <x:template match="tcx:Id">
	<x:copy>
	  <x:apply-templates select="@*|node()"/>
	</x:copy>
	<x:if test="not(../tcx:Drag)"><Drag><x:value-of select="$Drag"/></Drag></x:if>
	<x:if test="not(../tcx:Action)"><Action><x:value-of select="$Action"/></Action></x:if>
	<x:if test="not(../tcx:Subject)"><Subject>Rob</Subject></x:if>
	<x:if test="not(../tcx:Locus)"><Locus>Home</Locus></x:if>
	<x:if test="not(../tcx:Refine)"><Refine><x:value-of select="$Refine"/></Refine></x:if>
	<x:if test="not(../tcx:Detail)"><Detail><x:value-of select="$Detail"/></Detail></x:if>
	<x:if test="not(../tcx:Temp)"><Temp><x:value-of select="$Temp"/></Temp></x:if>
	<x:if test="not(../tcx:Pres)"><Pres><x:value-of select="$Pres"/></Pres></x:if>
	<x:if test="not(../tcx:Humi)"><Pres><x:value-of select="$Humi"/></Pres></x:if>
  </x:template>

</x:stylesheet>
