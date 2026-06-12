/// resource_id must start with /subscriptions/... (ARM format). Do not double-prefix.
pub fn resource_url(resource_id: &str) -> String {
    format!("https://portal.azure.com/#resource{}", resource_id)
}

pub fn resource_group_url(subscription_id: &str, name: &str) -> String {
    format!(
        "https://portal.azure.com/#resource/subscriptions/{}/resourceGroups/{}",
        subscription_id, name
    )
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn resource_url_no_double_prefix() {
        let id = "/subscriptions/abc/resourceGroups/rg/providers/Microsoft.Web/sites/mysite";
        let url = resource_url(id);
        assert_eq!(
            url,
            "https://portal.azure.com/#resource/subscriptions/abc/resourceGroups/rg/providers/Microsoft.Web/sites/mysite"
        );
        assert!(!url.contains("#resource/subscriptions") || url.matches("#resource").count() == 1);
    }

    #[test]
    fn resource_group_url_correct_format() {
        let url = resource_group_url("sub-123", "my-rg");
        assert_eq!(
            url,
            "https://portal.azure.com/#resource/subscriptions/sub-123/resourceGroups/my-rg"
        );
    }
}
