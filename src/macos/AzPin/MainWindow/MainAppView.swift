import SwiftUI

struct MainAppView: View {
    @State private var selectedGroup: PinnedResourceGroup?
    @State private var showOnboarding = !UserDefaults.standard.bool(forKey: "hasCompletedOnboarding")
    @Environment(OnboardingViewModel.self) private var onboardingVM

    var body: some View {
        NavigationSplitView {
            SidebarView(selectedGroup: $selectedGroup)
        } detail: {
            DetailView(selectedGroup: selectedGroup)
        }
        .sheet(isPresented: $showOnboarding) {
            OnboardingView()
                .environment(onboardingVM)
        }
    }
}
