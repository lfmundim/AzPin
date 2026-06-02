import SwiftUI

struct MainAppView: View {
    @State private var selectedGroup: PinnedResourceGroup?
    @State private var showOnboarding = !UserDefaults.standard.bool(forKey: "hasCompletedOnboarding")
    @Environment(OnboardingViewModel.self) private var onboardingVM
    @Environment(AuthViewModel.self) private var auth
    @Environment(BrowseViewModel.self) private var browseVM

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
        .onChange(of: showOnboarding) { _, isShowing in
            guard !isShowing else { return }
            Task {
                await auth.refresh()
                await browseVM.loadSubscriptions()
            }
        }
    }
}
