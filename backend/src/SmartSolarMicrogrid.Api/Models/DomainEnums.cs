/*
 * File: src/SmartSolarMicrogrid.Api/Models/DomainEnums.cs
 * Project: Smart Solar Microgrid Trading System
 * Purpose: Persistence model definitions for Domain Enums.
 */
namespace SmartSolarMicrogrid.Api.Models;

public enum UserRole { PROSUMER, BACKOFFICE, GRID_OPERATOR }
public enum UserStatus { PENDING, ACTIVE, DEACTIVATION_REQUESTED, DEACTIVATED }
public enum StationStatus { INACTIVE, ACTIVE, MAINTENANCE, DEACTIVATED }
public enum SlotStatus { CLOSED, OPEN, CANCELLED }
public enum ReservationStatus { PENDING, APPROVED, CANCELLED, COMPLETED, REJECTED }
