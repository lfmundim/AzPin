pub const RUNNABLE_TYPES: &[&str] = &[
    "microsoft.web/sites",
    "microsoft.web/sites/slots",
    "microsoft.app/containerapps",
    "microsoft.logic/workflows",
    "microsoft.compute/virtualmachines",
];

pub fn is_runnable(resource_type: &str) -> bool {
    let lower = resource_type.to_lowercase();
    RUNNABLE_TYPES.iter().any(|&t| t == lower)
}

#[derive(Debug, Clone, PartialEq)]
pub enum ResourceState {
    Running,
    Stopped,
    Starting,
    Stopping,
    Restarting,
    Unknown,
}

impl ResourceState {
    pub fn from_str(s: &str) -> Self {
        match s.to_lowercase().as_str() {
            "running" | "succeeded" | "enabled" => Self::Running,
            "stopped" | "deallocated" | "disabled" | "stopped (deallocated)" => Self::Stopped,
            "starting" => Self::Starting,
            "stopping" => Self::Stopping,
            "restarting" => Self::Restarting,
            _ => Self::Unknown,
        }
    }

    pub fn is_running(&self) -> bool {
        matches!(self, Self::Running)
    }

    pub fn is_stopped(&self) -> bool {
        matches!(self, Self::Stopped)
    }

    pub fn is_transitioning(&self) -> bool {
        matches!(self, Self::Starting | Self::Stopping | Self::Restarting)
    }

    pub fn display_label(&self) -> &'static str {
        match self {
            Self::Running => "Running",
            Self::Stopped => "Stopped",
            Self::Starting => "Starting",
            Self::Stopping => "Stopping",
            Self::Restarting => "Restarting",
            Self::Unknown => "Unknown",
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn is_runnable_all_five_types() {
        assert!(is_runnable("microsoft.web/sites"));
        assert!(is_runnable("microsoft.web/sites/slots"));
        assert!(is_runnable("microsoft.app/containerapps"));
        assert!(is_runnable("microsoft.logic/workflows"));
        assert!(is_runnable("microsoft.compute/virtualmachines"));
    }

    #[test]
    fn is_runnable_case_insensitive() {
        assert!(is_runnable("Microsoft.Web/Sites"));
        assert!(is_runnable("MICROSOFT.COMPUTE/VIRTUALMACHINES"));
        assert!(is_runnable("Microsoft.App/ContainerApps"));
    }

    #[test]
    fn is_runnable_false_for_non_runnable() {
        assert!(!is_runnable("microsoft.storage/storageaccounts"));
        assert!(!is_runnable("microsoft.network/virtualnetworks"));
        assert!(!is_runnable("microsoft.compute/disks"));
        assert!(!is_runnable("microsoft.compute/snapshots"));
    }

    #[test]
    fn resource_state_from_str_running_variants() {
        assert_eq!(ResourceState::from_str("Running"), ResourceState::Running);
        assert_eq!(ResourceState::from_str("Succeeded"), ResourceState::Running);
        assert_eq!(ResourceState::from_str("Enabled"), ResourceState::Running);
        assert_eq!(ResourceState::from_str("running"), ResourceState::Running);
    }

    #[test]
    fn resource_state_from_str_stopped_variants() {
        assert_eq!(ResourceState::from_str("Stopped"), ResourceState::Stopped);
        assert_eq!(ResourceState::from_str("Deallocated"), ResourceState::Stopped);
        assert_eq!(ResourceState::from_str("Disabled"), ResourceState::Stopped);
        assert_eq!(ResourceState::from_str("Stopped (Deallocated)"), ResourceState::Stopped);
    }

    #[test]
    fn resource_state_from_str_transitioning() {
        assert_eq!(ResourceState::from_str("Starting"), ResourceState::Starting);
        assert_eq!(ResourceState::from_str("Stopping"), ResourceState::Stopping);
        assert_eq!(ResourceState::from_str("Restarting"), ResourceState::Restarting);
    }

    #[test]
    fn resource_state_from_str_unknown() {
        assert_eq!(ResourceState::from_str("SomeRandomState"), ResourceState::Unknown);
        assert_eq!(ResourceState::from_str(""), ResourceState::Unknown);
        assert_eq!(ResourceState::from_str("Provisioning"), ResourceState::Unknown);
    }
}
